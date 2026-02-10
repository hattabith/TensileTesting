using System.Runtime.Intrinsics.Arm;
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

        public MainPage()
        {
            InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {


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
                OutputTextBlock.Text += $"Error: {ex.Message}\n";
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
                            OutputTextBlock.Text += "-> " + command + '\n';
                        });

                        _sp.WriteToPort(command, 1000);
                        var data = _sp.ReadFromPort(300);

                        // оновити UI:
                        await Dispatcher.InvokeAsync(() =>
                        {
                            OutputTextBlock.Text += "<- " + data + '\n';
                        });
                    }
                }
                catch (Exception ex)
                {
                    // лог
                }

                await Task.Delay(500, token); // інтервал опитування
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
                    break;

                case ConnectionState.Connecting:
                case ConnectionState.Initializing:
                    ConnectButton.Content = "Connecting...";
                    ConnectButton.IsEnabled = false;
                    COMPortComboBox.IsEnabled = false;
                    BaudRateComboBox.IsEnabled = false;
                    Address485ComboBox.IsEnabled = false;
                    break;

                case ConnectionState.Connected:
                    ConnectButton.Content = "Disconnect";
                    ConnectButton.IsEnabled = true;
                    break;
            }
        }

    }
}
