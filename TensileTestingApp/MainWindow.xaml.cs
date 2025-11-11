using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using MccDaq;
using System.Diagnostics;

namespace TensileTestingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serialPort = new SerialPort();
        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine("Starting App... ");
            LoadComPort();
            OpenCOM.IsEnabled = true;
            CloseCOM.IsEnabled = false;
        }


        private void LoadComPort()
        {
            string[] ports = SerialPort.GetPortNames();
            Debug.Write("Avaliable COM ports: ");
            foreach (string port in ports)
            {
                ComPortComboBox.Items.Add(port);
                Debug.Write(port + " ");
            }
            Debug.WriteLine("");
            
            ComPortComboBox.SelectedIndex = ports.Length - 1;
        }


        private void IntValue(object sender, DependencyPropertyChangedEventArgs e)
        {

        }

        private void OpenCOM_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               serialPort.PortName = ComPortComboBox.SelectedItem.ToString();
               serialPort.BaudRate = Convert.ToInt32(ComPortComboBox.SelectedIndex);
               serialPort.DataBits = 8;
               serialPort.Parity = Parity.None;
               serialPort.StopBits = StopBits.One;

                OpenCOM.IsEnabled = false;
                CloseCOM.IsEnabled = true;
                StatusBarText.Text = "COM Port Open";
                Debug.WriteLine("COM port open");
            }
            catch(Exception err) 
            {
                MessageBox.Show(err.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void CloseCOM_Click(object sender, RoutedEventArgs e)
        {
            serialPort.Close();
            OpenCOM.IsEnabled = true;
            CloseCOM.IsEnabled = false;
            StatusBarText.Text = "COM Port Closed";
            Debug.WriteLine("COM port close");
        }
    }
}