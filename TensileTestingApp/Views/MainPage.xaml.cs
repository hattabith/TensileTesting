using System.Windows.Controls;
using TensileTestingApp.Models;

namespace TensileTestingApp.Views
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var sp = new SerialPortCommunications(COMPortComboBox.SelectedItem.ToString(), (Int32)BaudRateComboBox.SelectedItem, Address485ComboBox.SelectedIndex);
            sp.OpenConnection();
        }
    }
}
