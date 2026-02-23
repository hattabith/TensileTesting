using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Controls;
using TensileTestingApp.Models;
using static TensileTestingApp.ViewModel.MainWindowViewModel;

namespace TensileTestingApp.Views
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        private SerialPortCommunications? _sp;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private CancellationTokenSource? _pollCts;
        private Task? _pollTask;
        private DCONProtocol? _dCon;
        private ObservableCollection<TensileTestData> _testData;
        private ResiveingToFileState _resiveState = ResiveingToFileState.Stopped;
        private string _fileName = null;
        private StreamWriter _writer;

        public MainPage()
        {
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
                _connectionState = ConnectionState.Error;
                UpdateUiState();
            }

        }
        private async Task ConnectAsync()
        {
            _connectionState = ConnectionState.Connecting;
            UpdateUiState();


            _sp = new SerialPortCommunications(
                COMPortComboBox.SelectedItem.ToString(),
                (int)BaudRateComboBox.SelectedItem,
                Address485ComboBox.SelectedIndex);

            await Task.Run(() => _sp.OpenConnection());


            _connectionState = ConnectionState.Connected;
            UpdateUiState();

        }
        private async Task DisconnectAsync()
        {
            _connectionState = ConnectionState.Disconnecting;
            UpdateUiState();

            await Task.Run(() => _sp.CloseConnection());
            _sp = null;

            _connectionState = ConnectionState.Disconnected;
            UpdateUiState();
        }
        private void StartPolling()
        {
            if (_pollTask != null && !_pollTask.IsCompleted)
                return;

            _dCon = new DCONProtocol(_sp.GetDeviceAddress());
            _pollCts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoop(_pollCts.Token));
        }
        private async Task PollLoop(CancellationToken token)
        {
            string resivedData = null;
            ADCDataParser parser = new ADCDataParser();
            while (!token.IsCancellationRequested)
            {

                try
                {
                    if (_sp != null && _dCon != null && _connectionState == ConnectionState.Connected)
                    {
                        string command = _dCon.GetReadCommand();

                        // оновити UI:
                        await Dispatcher.InvokeAsync(() =>
                        {
                            OutputTextBox.Text += command + '\n';
                            OutputScrollViewer.ScrollToEnd();
                        });

                        _sp.WriteToPort(command, 1000);
                        var data = _sp.ReadFromPort(300);

                        // оновити UI:
                        await Dispatcher.InvokeAsync(() =>
                        {
                            resivedData = DateTime.Now.ToString("o", CultureInfo.InvariantCulture) + " " + data + '\n';
                            OutputTextBox.Text += resivedData;
                            OutputScrollViewer.ScrollToEnd();
                            ForceDSeg7.Text = parser.ParseWithOutCheckSum(resivedData).Force.ToString("F");
                            LengthDSeg7.Text = parser.ParseWithOutCheckSum(resivedData).Length.ToString("F");
                            string line = $"{parser.ParseWithOutCheckSum(resivedData).Timestamp.ToString("hh:mm:ss.ffff"):O};{parser.ParseWithOutCheckSum(resivedData).Force.ToString():F3};{parser.ParseWithOutCheckSum(resivedData).Length.ToString():F3}";
                            if (_resiveState == ResiveingToFileState.Reciveing)
                            {
                                _writer.WriteLineAsync(line);
                                _writer.FlushAsync();
                            }


                        });
                    }
                }
                catch (Exception ex)
                {
                    // лог
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
                case ResiveingToFileState.Stopped:
                    RecordButton.Content = "Start";
                    break;
                case ResiveingToFileState.Reciveing:
                    RecordButton.Content = "Stop";
                    break;
            }
        }

        private async void RecordButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_resiveState == ResiveingToFileState.Stopped)
            {
                _resiveState = ResiveingToFileState.Reciveing;
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


            }
            else
            {
                _resiveState = ResiveingToFileState.Stopped;
                UpdateUiState();
                await _writer.FlushAsync();
                _writer.Close();
            }
        }
    }
}
