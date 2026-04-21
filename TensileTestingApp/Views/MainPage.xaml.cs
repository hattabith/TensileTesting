using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Windows.Controls;
using TensileTestingApp.Configuration;
using TensileTestingApp.Models;
using TensileTestingApp.Services.Abstractions;
using TensileTestingApp.Services.Implementations;
using TensileTestingApp.ViewModel;
using static TensileTestingApp.ViewModel.MainWindowViewModel;

namespace TensileTestingApp.Views;
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private readonly ISerialPortService _serialPortService;
        private readonly IDconProtocolService _dconProtocolService;
        private readonly IDataParser _dataParser;
        private readonly ILogger _logger;
        private readonly AppSettings _settings;
        private readonly ISignalFilter _forceFilter;
        private readonly ISignalFilter _lengthFilter;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private CancellationTokenSource? _pollCts;
        private Task? _pollTask;
        private readonly ObservableCollection<TensileTestData> _testData = new();
        private ReceivingToFileState _resiveState = ReceivingToFileState.Stopped;
        private string? _fileName;
        private StreamWriter? _writer;
        private DateTime _lastFlushUtc = DateTime.UtcNow;
        private Channel<TensileTestData>? _writeChannel;
        private Task? _writerTask;
        private CancellationTokenSource? _writerCts;

        public MainPage(AppSettings settings)
            : this(
                new SerialPortService(),
                new DconProtocolService(),
                new AdcDataParserService(settings.Parser),
                new AppLogger(settings.Logging),
                settings)
        {
        }

        public MainPage(
            ISerialPortService serialPortService,
            IDconProtocolService dconProtocolService,
            IDataParser dataParser,
            ILogger logger,
            AppSettings settings)
        {
            _serialPortService = serialPortService;
            _dconProtocolService = dconProtocolService;
            _dataParser = dataParser;
            _logger = logger;
            _settings = settings;
            _forceFilter = CreateFilter(settings.Filter);
            _lengthFilter = CreateFilter(settings.LengthFilter);
            InitializeComponent();
            Unloaded += MainPage_Unloaded;
        }

        private static ISignalFilter CreateFilter(FilterSettings settings)
        {
            if (!settings.EnableForceFilter)
                return new ExponentialMovingAverageFilter(1.0); // alpha=1 → passthrough

            return settings.Type.ToUpperInvariant() switch
            {
                "SG" or "SAVGOL" or "SAVITZKYGOLAY" => new SavitzkyGolayFilter(settings.SavitzkyGolayWindow),
                "MA" or "MOVINGAVERAGE" => new MovingAverageFilter(Math.Max(1, settings.MovingAverageWindow)),
                _ => new ExponentialMovingAverageFilter(Math.Clamp(settings.EmaAlpha, 0.001, 1.0))
            };
        }

        private async void MainPage_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await CleanupResourcesAsync();
        }

        private async void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CultureInfo configuredCulture = new(_settings.Acquisition.Culture);
            CultureInfo.CurrentCulture = configuredCulture;
            CultureInfo.CurrentUICulture = configuredCulture;

            // треба зробити поток для ініціалізації з'єднання, після того як ініціалізація успішно відбулася,
            // тоді робимо поток читання і виводу в текстове поле

            try
            {
                if (_connectionState == ConnectionState.Disconnected)
                {
                    await ConnectAsync();
                    StartPolling();
                }
                else if (_connectionState == ConnectionState.Connected)
                {
                    await DisconnectAsync();
                    await StopPolling();
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.Text += $"Error: {ex.Message}\n";
                _logger.LogError("Connection button operation failed", ex);
                _connectionState = ConnectionState.Error;
                UpdateUiState();
            }

        }
        private async Task ConnectAsync()
        {
            _connectionState = ConnectionState.Connecting;
            UpdateUiState();

            _serialPortService.Configure(
                COMPortComboBox.SelectedItem?.ToString() ?? _settings.SerialPort.DefaultPortName,
                (int?)BaudRateComboBox.SelectedItem ?? _settings.SerialPort.DefaultBaudRate,
                Address485ComboBox.SelectedIndex >= 0 ? Address485ComboBox.SelectedIndex : _settings.SerialPort.DefaultDeviceAddress);

            await Task.Run(_serialPortService.OpenConnection);


            _connectionState = ConnectionState.Connected;
            _logger.LogInfo($"Connected to {_serialPortService.PortName} with baud {_serialPortService.BaudRate}");
            UpdateUiState();

        }
        private async Task DisconnectAsync()
        {
            _connectionState = ConnectionState.Disconnecting;
            UpdateUiState();

            await Task.Run(_serialPortService.CloseConnection);

            _connectionState = ConnectionState.Disconnected;
            _logger.LogInfo("Disconnected from serial port");
            UpdateUiState();
        }
        private void StartPolling()
        {
            if (_pollTask != null && !_pollTask.IsCompleted)
                return;

            _dconProtocolService.SetAddress(_serialPortService.DeviceAddress);
            _pollCts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoop(_pollCts.Token));
        }
        private async Task PollLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {

                try
                {
                    if (_serialPortService.IsOpen && _connectionState == ConnectionState.Connected)
                    {
                        string command = _dconProtocolService.GetReadCommand();

                        // оновити UI:
                        await Dispatcher.InvokeAsync(() =>
                        {
                            OutputTextBox.Text += command + '\n';
                            OutputScrollViewer.ScrollToEnd();
                        });

                        _serialPortService.WriteToPort(command, _settings.SerialPort.WriteTimeoutMs);
                        var data = _serialPortService.ReadFromPort(_settings.SerialPort.ReadTimeoutMs);
                        string receivedData = DateTime.Now.ToString("o", CultureInfo.InvariantCulture) + " " + data + '\n';
                        TensileTestData parsedData = _settings.SerialPort.UseChecksum
                            ? _dataParser.ParseWithChecksum(receivedData)
                            : _dataParser.ParseWithoutChecksum(receivedData);

                        parsedData.FilteredForce = _forceFilter.Filter(parsedData.Force);
                        parsedData.FilteredLength = _lengthFilter.Filter(parsedData.Length);

                        string line = string.Join(
                            _settings.Recording.Delimiter,
                            parsedData.Timestamp.ToString(_settings.Ui.DateTimeFormat, CultureInfo.InvariantCulture),
                            parsedData.Force.ToString("F3", CultureInfo.InvariantCulture),
                            parsedData.Length.ToString("F3", CultureInfo.InvariantCulture));

                        // оновити UI:
                        await Dispatcher.InvokeAsync(() =>
                        {
                            OutputTextBox.Text += receivedData;
                            OutputScrollViewer.ScrollToEnd();
                            ForceDSeg7.Text = parsedData.Force.ToString(_settings.Ui.ForceDisplayFormat, CultureInfo.InvariantCulture);
                            LengthDSeg7.Text = parsedData.Length.ToString(_settings.Ui.LengthDisplayFormat, CultureInfo.InvariantCulture);
                        });

                        if (_resiveState == ReceivingToFileState.Receiving)
                        {
                            await Dispatcher.InvokeAsync(() => AppendDataPoint(parsedData));
                            await EnqueueForFileWriteAsync(parsedData, token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Polling loop failed", ex);
                }

                await Task.Delay(_settings.SerialPort.PollingIntervalMs, token);
            }
        }
        private async Task StopPolling()
        {
            if (_pollCts == null) return;

            _pollCts.Cancel();

            try
            {
                if (_pollTask != null)
                    await _pollTask;
            }
            catch (OperationCanceledException) { }

            _pollCts.Dispose();
            _pollCts = null;
            _pollTask = null;
        }

        private void UpdateUiState()
        {
            switch (_connectionState)
            {
                case ConnectionState.Disconnected:
                    ConnectButton.Content = "Connect";
                    ConnectButton.IsEnabled = true;
                    COMPortComboBox.IsEnabled = true;
                    BaudRateComboBox.IsEnabled = true;
                    Address485ComboBox.IsEnabled = true;
                    RecordButton.IsEnabled = false;
                    FileNameTextBox.IsEnabled = false;
                    break;

                case ConnectionState.Connecting:
                case ConnectionState.Initializing:
                    ConnectButton.Content = "Connecting...";
                    ConnectButton.IsEnabled = false;
                    COMPortComboBox.IsEnabled = false;
                    BaudRateComboBox.IsEnabled = false;
                    Address485ComboBox.IsEnabled = false;
                    RecordButton.IsEnabled = false;
                    FileNameTextBox.IsEnabled = false;
                    break;

                case ConnectionState.Connected:
                    ConnectButton.Content = "Disconnect";
                    ConnectButton.IsEnabled = true;
                    RecordButton.IsEnabled = true;
                    FileNameTextBox.IsEnabled = true;
                    break;
            }
            switch (_resiveState)
            {
                case ReceivingToFileState.Stopped:
                    RecordButton.Content = "Start";
                    break;
                case ReceivingToFileState.Receiving:
                    RecordButton.Content = "Stop";
                    break;
            }
        }

        private async void RecordButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_resiveState == ReceivingToFileState.Stopped)
            {
                await StartRecordingAsync();


            }
            else
            {
                await StopRecordingAsync();
            }
        }

        private async Task CleanupResourcesAsync()
        {
            try
            {
                await StopPolling();

                if (_serialPortService.IsOpen)
                {
                    await Task.Run(_serialPortService.CloseConnection);
                }

                if (_resiveState == ReceivingToFileState.Receiving || _writerTask is not null || _writer is not null)
                {
                    await StopRecordingAsync();
                }

                _writeChannel = null;
                _writerTask = null;
                _writerCts?.Dispose();
                _writerCts = null;

                _resiveState = ReceivingToFileState.Stopped;
                _connectionState = ConnectionState.Disconnected;
            }
            catch (Exception ex)
            {
                _logger.LogError("Cleanup resources failed", ex);
            }
        }

        private async Task StartRecordingAsync()
        {
            _resiveState = ReceivingToFileState.Receiving;
            UpdateUiState();

            _testData.Clear();
            _forceFilter.Reset();
            _lengthFilter.Reset();
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ResetLiveData();
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string experimentName = $"{timestamp}_{FileNameTextBox.Text}";
            string rootFolder = Environment.ExpandEnvironmentVariables(_settings.Recording.BaseFolder);
            string baseDir = Path.Combine(rootFolder, experimentName);

            Directory.CreateDirectory(baseDir);

            string filePath = Path.Combine(baseDir, $"{experimentName}.csv");
            _fileName = filePath;
            _lastFlushUtc = DateTime.UtcNow;

            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding(_settings.Recording.FileEncoding);
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            _writer = new StreamWriter(filePath, true, encoding);
            await _writer.WriteLineAsync(_settings.Recording.Header);
            await _writer.FlushAsync();

            _writerCts = new CancellationTokenSource();
            _writeChannel = Channel.CreateUnbounded<TensileTestData>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });
            _writerTask = Task.Run(() => RunBatchWriterAsync(_writer, _writeChannel.Reader, _writerCts.Token));

            _logger.LogInfo($"Started recording to {_fileName}");
        }

        private async Task StopRecordingAsync()
        {
            _resiveState = ReceivingToFileState.Stopped;
            UpdateUiState();

            if (_writeChannel is not null)
            {
                _writeChannel.Writer.TryComplete();
            }

            if (_writerTask is not null)
            {
                try
                {
                    await _writerTask;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Batch writer task failed", ex);
                }
            }

            _writerCts?.Cancel();
            _writerCts?.Dispose();
            _writerCts = null;
            _writeChannel = null;
            _writerTask = null;

            if (_writer is not null)
            {
                await _writer.FlushAsync();
                _writer.Dispose();
                _writer = null;
            }

            _logger.LogInfo($"Stopped recording. Points collected: {_testData.Count}");
        }

        private void AppendDataPoint(TensileTestData data)
        {
            _testData.Add(data);
            if (_testData.Count > _settings.Acquisition.MaxUiBufferLines)
            {
                _testData.RemoveAt(0);
            }

            if (DataContext is MainWindowViewModel vm)
            {
                vm.AddLiveDataPoint(data);
            }
        }

        private async Task EnqueueForFileWriteAsync(TensileTestData data, CancellationToken token)
        {
            if (_writeChannel is null)
            {
                return;
            }

            await _writeChannel.Writer.WriteAsync(data, token);
        }

        private async Task RunBatchWriterAsync(StreamWriter writer, ChannelReader<TensileTestData> reader, CancellationToken token)
        {
            int batchSize = Math.Max(1, _settings.Recording.BatchSizePoints);
            List<TensileTestData> batch = new(batchSize);

            while (await reader.WaitToReadAsync(token))
            {
                while (reader.TryRead(out TensileTestData? item))
                {
                    batch.Add(item);
                    if (batch.Count >= batchSize)
                    {
                        await WriteBatchAsync(writer, batch);
                        batch.Clear();
                    }
                }
            }

            if (batch.Count > 0)
            {
                await WriteBatchAsync(writer, batch);
            }
        }

        private async Task WriteBatchAsync(StreamWriter writer, List<TensileTestData> batch)
        {
            StringBuilder builder = new();
            foreach (TensileTestData data in batch)
            {
                string line = string.Join(
                    _settings.Recording.Delimiter,
                    data.Timestamp.ToString(_settings.Ui.DateTimeFormat, CultureInfo.InvariantCulture),
                    data.Force.ToString("F3", CultureInfo.InvariantCulture),
                    data.FilteredForce.ToString("F3", CultureInfo.InvariantCulture),
                    data.Length.ToString("F3", CultureInfo.InvariantCulture),
                    data.FilteredLength.ToString("F3", CultureInfo.InvariantCulture));
                builder.AppendLine(line);
            }

            await writer.WriteAsync(builder.ToString());

            if (_settings.Recording.AutoFlush)
            {
                await writer.FlushAsync();
                _lastFlushUtc = DateTime.UtcNow;
                return;
            }

            if ((DateTime.UtcNow - _lastFlushUtc).TotalMilliseconds >= _settings.Recording.FlushIntervalMs)
            {
                await writer.FlushAsync();
                _lastFlushUtc = DateTime.UtcNow;
            }
        }
    }
