using System.Windows.Controls;
using TensileTestingApp.Models;

namespace TensileTestingApp.Views
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        SerialPortCommunications sp;
        public MainPage()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            sp = new SerialPortCommunications(COMPortComboBox.SelectedItem.ToString(), (Int32)BaudRateComboBox.SelectedItem, Address485ComboBox.SelectedIndex);
            if (!sp.IsOpen)
            {
                // TODO: Need refactoring

                //sp = new SerialPortCommunications(COMPortComboBox.SelectedItem.ToString(), (Int32)BaudRateComboBox.SelectedItem, Address485ComboBox.SelectedIndex);
                sp.OpenConnection();
                ConnectButton.Content = "Disconnect";
                COMPortComboBox.IsEnabled = false;
                BaudRateComboBox.IsEnabled = false;
                Address485ComboBox.IsEnabled = false;
                ConnectButton.IsEnabled = false;
                OutputTextBlock.Text += "Connected to " + sp.GetPortName() + " at " + sp.GetBaudRate().ToString() + " baud, address " + sp.GetDeviceAddress().ToString() + "\n";
                Task.Delay(1000);
                ConnectButton.IsEnabled = true;
                OutputTextBlock.Text += "Sending: #02\n";
                sp.WriteToPort("#02\n", 300);
                OutputTextBlock.Text += "Response is: " + sp.ReadFromPort().ToString();
            }
            else
            {
                //sp = new SerialPortCommunications(COMPortComboBox.SelectedItem.ToString(), (Int32)BaudRateComboBox.SelectedItem, Address485ComboBox.SelectedIndex);
                sp.CloseConnection();
                ConnectButton.Content = "Connect";
                COMPortComboBox.IsEnabled = true;
                BaudRateComboBox.IsEnabled = true;
                Address485ComboBox.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                Task.Delay(1000);
            }
            //var sp = new SerialPortCommunications(COMPortComboBox.SelectedItem.ToString(), (Int32)BaudRateComboBox.SelectedItem, Address485ComboBox.SelectedIndex);
            //sp.OpenConnection();
            //ConnectButton.Content = "Disconnect";
            //ConnectButton.IsEnabled = false;
            //await Task.Delay(1000);
            //ConnectButton.IsEnabled = true;
        }
    }
}
