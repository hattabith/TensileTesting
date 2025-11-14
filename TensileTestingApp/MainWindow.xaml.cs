using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;

namespace TensileTestingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        SerialPort serialPort = new SerialPort();
        public bool comPortIsOpen;

        private readonly ObservableCollection<double> _ch1 = new();
        private readonly ObservableCollection<double> _ch2 = new();
        private readonly System.Timers.Timer _timer;
        private double _x = 0;

        public ISeries[] Series { get; set; }
        public Axis[] XAxes { get; set; }
        public Axis[] YAxes { get; set; }

        public MainWindow()
        {

            DataContext = this;

            Series = new ISeries[]
            {
                new LineSeries<double> { Values = _ch1, Name = "Channel 1", Stroke = new SolidColorPaint(SKColors.Orange, 2), GeometryFill = null, GeometryStroke = null },
                new LineSeries<double> { Values = _ch2, Name = "Channel 2", Stroke = new SolidColorPaint(SKColors.DeepSkyBlue, 2), GeometryFill = null, GeometryStroke = null }
            };

            XAxes = new Axis[] { new() { Name = "Time", MinLimit = 0, MaxLimit = 100 } };
            YAxes = new Axis[] { new() { Name = "Amplitude" } };

            _timer = new System.Timers.Timer(200); // 10 samples per second
            _timer.Elapsed += (s, e) => Dispatcher.Invoke(UpdateData);
            _timer.Start();

            InitializeComponent();
            Debug.WriteLine("Starting App... ");
            LoadComPort();
            OpenCOM.IsEnabled = true;
            CloseCOM.IsEnabled = false;
            comPortIsOpen = false;


        }

        // Change the event declaration to nullable to fix CS8618
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


        private void UpdateData()
        {
            // Симуляція двох каналів
            _ch1.Add(Math.Sin(_x / 10) + 0.1 * Random.Shared.NextDouble());
            _ch2.Add(Math.Cos(_x / 10) + 0.1 * Random.Shared.NextDouble());
            _x++;

            if (_ch1.Count > 100)
            {
                _ch1.RemoveAt(0);
                _ch2.RemoveAt(0);
            }

            XAxes[0].MinLimit = _x - 100;
            XAxes[0].MaxLimit = _x;
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
                comPortIsOpen = true;
            }
            catch (Exception err)
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
            comPortIsOpen = false;
        }
    }
}