using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Controls;
using TensileTestingApp.Models;
using TensileTestingApp.Services.Abstractions;
using TensileTestingApp.Services.Implementations;
using static TensileTestingApp.ViewModel.MainWindowViewModel;

namespace TensileTestingApp.Views
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private readonly ISerialPortService _serialPortService;
        private readonly IDconProtocolService _dconProtocolService;
        private readonly IDataParser _dataParser;
        private readonly ILogger _logger;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private CancellationTokenSource? _pollCts;
        private Task? _pollTask;
        private ObservableCollection<TensileTestData> _testData;
        private ReceivingToFileState _resiveState = ReceivingToFileState.Stopped;
        private string? _fileName;
        private StreamWriter? _writer;

        public MainPage()
            : this(new SerialPortService(), new DconProtocolService(), new AdcDataParserService(), new AppLogger())
        {
        }

        public MainPage(
            ISerialPortService serialPortService,
            IDconProtocolService dconProtocolService,
            IDataParser dataParser,
            ILogger logger)
        {
            _serialPortService = serialPortService;
            _dconProtocolService = dconProtocolService;
            _dataParser = dataParser;
            _logger = logger;
            InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

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
                COMPortComboBox.SelectedItem?.ToString() ?? string.Empty,
                (int)BaudRateComboBox.SelectedItem,
                Address485ComboBox.SelectedIndex);

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
            string resivedData = null;
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

                        _serialPortService.WriteToPort(command, 1000);
                        var data = _serialPortService.ReadFromPort(300);
                        TensileTestData parsedData = _dataParser.ParseWithoutChecksum(DateTime.Now.ToString("o", CultureInfo.InvariantCulture) + " " + data);

                        // оновити UI:
                        await Dispatcher.InvokeAsync(() =>
                        {
                            resivedData = DateTime.Now.ToString("o", CultureInfo.InvariantCulture) + " " + data + '\n';
                            OutputTextBox.Text += resivedData;
                            OutputScrollViewer.ScrollToEnd();
                            ForceDSeg7.Text = parsedData.Force.ToString("F");
                            LengthDSeg7.Text = parsedData.Length.ToString("F");
                            string line = $"{parsedData.Timestamp.ToString("hh:mm:ss.ffff"):O};{parsedData.Force.ToString("F3"):F3};{parsedData.Length.ToString("F3"):F3}";
                            if (_resiveState == ReceivingToFileState.Reciveing && _writer is not null)
                            {
                                _ = _writer.WriteLineAsync(line);
                                _ = _writer.FlushAsync();
                            }


                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Polling loop failed", ex);
                }

                await Task.Delay(50, token); // інтервал опитування
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
                case ReceivingToFileState.Reciveing:
                    RecordButton.Content = "Stop";
                    break;
            }
        }

        private async void RecordButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_resiveState == ReceivingToFileState.Stopped)
            {
                _resiveState = ReceivingToFileState.Reciveing;
                UpdateUiState();
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string experimentName = $"{timestamp}_{FileNameTextBox.Text}";
                string baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "TensileTests",
                    experimentName);

                Directory.CreateDirectory(baseDir);

                string filePath = Path.Combine(baseDir, $"{experimentName}.csv");
                _fileName = filePath;
                _writer = new StreamWriter(filePath, true, Encoding.UTF8);
                await _writer.WriteLineAsync("DateTime;Force;Length");
                await _writer.FlushAsync();
                _logger.LogInfo($"Started recording to {_fileName}");


            }
            else
            {
                _resiveState = ReceivingToFileState.Stopped;
                UpdateUiState();
                if (_writer is not null)
                {
                    await _writer.FlushAsync();
                    _writer.Close();
                    _writer = null;
                }

                _logger.LogInfo("Stopped recording");
            }
        }
    }
}
