using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
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
        private readonly IZeroCorrectionService _zeroCorrectionService;
        private readonly IPreloadService _preloadService;
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

        public MainPage(AppSettings settings, IZeroCorrectionService zeroCorrectionService, IPreloadService preloadService)
            : this(
                new SerialPortService(),
                new DconProtocolService(),
                new AdcDataParserService(settings.Parser),
                new AppLogger(settings.Logging),
                settings,
                zeroCorrectionService,
                preloadService)
        {
        }

        public MainPage(
            ISerialPortService serialPortService,
            IDconProtocolService dconProtocolService,
            IDataParser dataParser,
            ILogger logger,
            AppSettings settings,
            IZeroCorrectionService zeroCorrectionService,
            IPreloadService preloadService)
        {
            _serialPortService = serialPortService;
            _dconProtocolService = dconProtocolService;
            _dataParser = dataParser;
            _logger = logger;
            _settings = settings;
            _forceFilter = CreateFilter(settings.Filter);
            _lengthFilter = CreateFilter(settings.LengthFilter);
            _zeroCorrectionService = zeroCorrectionService;
            _preloadService = preloadService;
            InitializeComponent();
            PreloadThresholdTextBox.Text = _preloadService.Threshold.ToString("F2", CultureInfo.InvariantCulture);
            PreloadModeComboBox.SelectedIndex = _preloadService.Mode == PreloadMode.OffsetSubtraction ? 0 : 1;
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
                    bool connected = ConnectAsync();
                    if (connected)
                    {
                        StartPolling();
                    }
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
                _connectionState = ConnectionState.Disconnected;
                UpdateUiState();
                ShowConnectionErrorDialog(ex);
            }

        }
        private bool ConnectAsync()
        {
            _connectionState = ConnectionState.Connecting;
            UpdateUiState();

            try
            {
                _serialPortService.Configure(
                    COMPortComboBox.SelectedItem?.ToString() ?? _settings.SerialPort.DefaultPortName,
                    (int?)BaudRateComboBox.SelectedItem ?? _settings.SerialPort.DefaultBaudRate,
                    Address485ComboBox.SelectedIndex >= 0 ? Address485ComboBox.SelectedIndex : _settings.SerialPort.DefaultDeviceAddress);

                _serialPortService.OpenConnection();
            }
            catch (Exception ex)
            {
                try
                {
                    if (_serialPortService.IsOpen)
                    {
                        _serialPortService.CloseConnection();
                    }
                }
                catch (Exception closeEx)
                {
                    _logger.LogError("Failed to close serial port after connection failure", closeEx);
                }

                _connectionState = ConnectionState.Disconnected;
                UpdateUiState();
                OutputTextBox.Text += $"Connection error: {GetConnectionErrorMessage(ex)}\n";
                _logger.LogError("Failed to connect to serial port", ex);

                ShowConnectionErrorDialog(ex);
                return false;
            }

            _connectionState = ConnectionState.Connected;
            _logger.LogInfo($"Connected to {_serialPortService.PortName} with baud {_serialPortService.BaudRate}");
            UpdateUiState();

            return true;
        }
        private async Task DisconnectAsync()
        {
            _connectionState = ConnectionState.Disconnecting;
            UpdateUiState();

            await Task.Run(_serialPortService.CloseConnection);

            if (_settings.ZeroCorrection.ClearOnDisconnect)
            {
                _zeroCorrectionService.Clear();
            }

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

        private void ShowConnectionErrorDialog(Exception ex)
        {
            string selectedPort = COMPortComboBox.SelectedItem?.ToString() ?? _settings.SerialPort.DefaultPortName;
            string message = $"Не вдалося відкрити COM-порт '{selectedPort}'.\n\n{GetConnectionErrorMessage(ex)}\n\nВиберіть інший порт та спробуйте ще раз.";

            System.Windows.MessageBox.Show(
                message,
                "Помилка підключення",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }

        private static string GetConnectionErrorMessage(Exception ex)
        {
            Exception rootCause = ex.InnerException ?? ex;

            return rootCause switch
            {
                UnauthorizedAccessException => "Порт зайнятий іншим застосунком або немає прав доступу.",
                IOException => "Порт не існує або зараз недоступний.",
                ArgumentException => "Вибрано некоректний COM-порт.",
                _ => rootCause.Message
            };
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

                        // Zero correction: feed sample while capturing; apply offset when ready
                        _zeroCorrectionService.AddSample(parsedData.FilteredForce, parsedData.FilteredLength);
                        parsedData.CorrectedForce = _zeroCorrectionService.ApplyForce(parsedData.FilteredForce);
                        parsedData.CorrectedLength = _zeroCorrectionService.ApplyLength(parsedData.FilteredLength);

                        // Preload tracking (only during an active recording)
                        if (_resiveState == ReceivingToFileState.Receiving)
                            _preloadService.ProcessSample(parsedData.CorrectedForce, parsedData.CorrectedLength);

                        parsedData.PreloadAdjustedForce = _preloadService.ApplyForce(parsedData.CorrectedForce);
                        parsedData.PreloadAdjustedLength = _preloadService.ApplyLength(parsedData.CorrectedLength);
                        parsedData.IsZeroApplied = _zeroCorrectionService.State == ZeroCorrectionState.Ready;
                        parsedData.IsPreloadApplied = _preloadService.State == PreloadState.ThresholdReached;

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

                            if (_zeroCorrectionService.State == ZeroCorrectionState.Ready)
                            {
                                CorrectedForceDSeg7.Text = parsedData.CorrectedForce.ToString(_settings.Ui.ForceDisplayFormat, CultureInfo.InvariantCulture);
                                CorrectedLengthDSeg7.Text = parsedData.CorrectedLength.ToString(_settings.Ui.LengthDisplayFormat, CultureInfo.InvariantCulture);
                            }

                            UpdateCalibrationStatusDisplay();
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
                    ZeroButton.IsEnabled = false;
                    ClearZeroButton.IsEnabled = false;
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
                    ZeroButton.IsEnabled = _zeroCorrectionService.State != ZeroCorrectionState.Capturing;
                    ClearZeroButton.IsEnabled = _zeroCorrectionService.State == ZeroCorrectionState.Ready;
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

                if (_settings.ZeroCorrection.ClearOnDisconnect)
                    _zeroCorrectionService.Clear();

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
            _preloadService.Reset();
            if (!_settings.ZeroCorrection.PreserveAcrossRecording)
                _zeroCorrectionService.Clear();
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

            // Write specimen parameters to companion JSON file
            var calibrationSnapshot = new
            {
                ZeroForceOffset = _zeroCorrectionService.ForceOffset,
                ZeroLengthOffset = _zeroCorrectionService.LengthOffset,
                ZeroQuality = _zeroCorrectionService.Quality.ToString(),
                ZeroEstablishedAt = _zeroCorrectionService.EstablishedAt?.ToString("O"),
                PreloadMode = _preloadService.Mode.ToString(),
                PreloadThreshold = _preloadService.Threshold
            };
            var specimenParams = new SpecimenParameters(
                Name: FileNameTextBox.Text,
                Type: SpecimenTypeComboBox.SelectedItem?.ToString() ?? "Unknown",
                DiameterMm: double.TryParse(DiameterTextBox.Text, out var d) ? d : 0,
                GaugeLengthMm: double.TryParse(GaugeLengthTextBox.Text, out var l) ? l : 0,
                RecordedAt: DateTime.Now
            );
            string metaPath = Path.Combine(baseDir, $"{experimentName}_meta.json");
            await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(
                new { Specimen = specimenParams, Calibration = calibrationSnapshot },
                new JsonSerializerOptions { WriteIndented = true }));

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

        private void ZeroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _zeroCorrectionService.StartCapture();
            ZeroButton.IsEnabled = false;
            ZeroButton.Content = "Capturing...";
            CorrectedForceDSeg7.Text = "—";
            CorrectedLengthDSeg7.Text = "—";
            _logger.LogInfo("Zero capture started");
        }

        private void ClearZeroButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _zeroCorrectionService.Clear();
            CorrectedForceDSeg7.Text = "—";
            CorrectedLengthDSeg7.Text = "—";
            UpdateCalibrationStatusDisplay();
            UpdateUiState();
            _logger.LogInfo("Zero correction cleared");
        }

        private void PreloadModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _preloadService.Mode = PreloadModeComboBox.SelectedIndex == 0
                ? PreloadMode.OffsetSubtraction
                : PreloadMode.OriginShift;
        }

        private void PreloadThresholdTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(PreloadThresholdTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedThreshold) || parsedThreshold < 0)
            {
                PreloadThresholdTextBox.Text = _preloadService.Threshold.ToString("F2", CultureInfo.InvariantCulture);
                return;
            }

            _preloadService.Threshold = parsedThreshold;
            _preloadService.Reset();
            UpdateCalibrationStatusDisplay();
        }

        private void UpdateCalibrationStatusDisplay()
        {
            // Must be called on the UI thread.
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ZeroOffsetText = $"F={_zeroCorrectionService.ForceOffset:F3}; L={_zeroCorrectionService.LengthOffset:F3}";
                vm.CalibrationQuality = _zeroCorrectionService.Quality.ToString();
                vm.PreloadStatusText = _preloadService.State == PreloadState.ThresholdReached
                    ? $"Locked @ {_preloadService.CapturedForceValue:F2} kN"
                    : "Waiting";
            }

            switch (_zeroCorrectionService.State)
            {
                case ZeroCorrectionState.Capturing:
                    int collected = _zeroCorrectionService.SamplesCollected;
                    int required = _zeroCorrectionService.SamplesRequired;
                    ZeroButton.Content = $"Capturing {collected}/{required}";
                    ZeroButton.IsEnabled = false;
                    ClearZeroButton.IsEnabled = false;
                    ForceOffsetText.Text = "…";
                    LengthOffsetText.Text = "…";
                    ZeroQualityDot.Foreground = new SolidColorBrush(Colors.DarkOrange);
                    ZeroQualityDot.ToolTip = "Capturing baseline";
                    ZeroTimestampText.Text = "";
                    break;

                case ZeroCorrectionState.Ready:
                    ZeroButton.Content = "Re-zero";
                    ZeroButton.IsEnabled = _connectionState == ConnectionState.Connected;
                    ClearZeroButton.IsEnabled = true;
                    ForceOffsetText.Text = _zeroCorrectionService.ForceOffset.ToString("F3", CultureInfo.InvariantCulture);
                    LengthOffsetText.Text = _zeroCorrectionService.LengthOffset.ToString("F3", CultureInfo.InvariantCulture);
                    ZeroQualityDot.Foreground = _zeroCorrectionService.Quality switch
                    {
                        ZeroQuality.Good    => new SolidColorBrush(Colors.LimeGreen),
                        ZeroQuality.Warning => new SolidColorBrush(Colors.DarkOrange),
                        ZeroQuality.Bad     => new SolidColorBrush(Colors.Red),
                        _                   => new SolidColorBrush(Colors.Gray)
                    };
                    ZeroQualityDot.ToolTip = $"Noise RMS: {_zeroCorrectionService.ForceNoiseRms:F4} kN ({_zeroCorrectionService.Quality})";
                    ZeroTimestampText.Text = _zeroCorrectionService.EstablishedAt?.ToString("HH:mm:ss");
                    break;

                default:
                    ZeroButton.Content = "Establish Zero";
                    ZeroButton.IsEnabled = _connectionState == ConnectionState.Connected;
                    ClearZeroButton.IsEnabled = false;
                    ForceOffsetText.Text = "—";
                    LengthOffsetText.Text = "—";
                    ZeroQualityDot.Foreground = new SolidColorBrush(Colors.Gray);
                    ZeroQualityDot.ToolTip = "No baseline set";
                    ZeroTimestampText.Text = "";
                    break;
            }

            if (_preloadService.State == PreloadState.ThresholdReached)
            {
                PreloadStatusText.Text = $"Locked @ {_preloadService.CapturedForceValue:F2} kN";
                PreloadStatusText.Foreground = new SolidColorBrush(Colors.DarkGreen);
            }
            else
            {
                PreloadStatusText.Text = "Waiting";
                PreloadStatusText.Foreground = new SolidColorBrush(Colors.Gray);
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
                    data.CorrectedForce.ToString("F3", CultureInfo.InvariantCulture),
                    data.PreloadAdjustedForce.ToString("F3", CultureInfo.InvariantCulture),
                    data.Length.ToString("F3", CultureInfo.InvariantCulture),
                    data.FilteredLength.ToString("F3", CultureInfo.InvariantCulture),
                    data.CorrectedLength.ToString("F3", CultureInfo.InvariantCulture),
                    data.PreloadAdjustedLength.ToString("F3", CultureInfo.InvariantCulture));
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
