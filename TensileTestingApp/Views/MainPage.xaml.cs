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
        public MainPage()
        {
            InitializeComponent();
        }

        private async void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {


            // треба зробити поток для ініціалізації з'єднання, після того як ініціалізація успішно відбулася,
            // тоді робимо поток читання і виводу в текстове поле
            // треба розібратися з змінною, яка відповідає за створення екземпляру класу SerialPortCommunications

            try
            {
                if (_connectionState == ConnectionState.Disconnected)
                {
                    await ConnectAsync();
                }
                else if (_connectionState == ConnectionState.Connected)
                {
                    await DisconnectAsync();
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

            _connectionState = ConnectionState.Initializing;
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
