using OxyPlot;
using OxyPlot.Series;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows.Controls;

namespace TensileTestingApp.ViewModel
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        private Frame _mainWindowFrame;


        public Frame MainWindowFrame
        {
            get { return _mainWindowFrame; }
            set
            {
                if (_mainWindowFrame != value)
                {
                    {
                        _mainWindowFrame = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MainWindowFrame)));

                    }
                }
            }
        }
        public Page CurrentPage { get; set; }

        public MainWindowViewModel()
        {
            this.ForcePlot = new PlotModel
            {
                Title = "Force Data"
            };
            var series1 = new LineSeries
            {
                Title = "Line Force",
                Color = OxyColors.Red
            };
            series1.Points.Add(new DataPoint(0, 1));
            series1.Points.Add(new DataPoint(10, 5));
            this.ForcePlot.Series.Add(series1);
            this.ForcePlot.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
            this.LengthPlot = new PlotModel { Title = "Length Data" };
            this.LengthPlot.Series.Add(new FunctionSeries(Math.Tan, 0, 10, 0.1, "tan(x)"));
            this.ForceLengthPlot = new PlotModel { Title = "Force/Length Data" };
            this.ForceLengthPlot.Series.Add(new FunctionSeries(Math.Exp, 0, 10, 0.1, "exp(x)"));
            this.PortList = SerialPort.GetPortNames().ToList<string>();
            this.RS485Address = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

        }
        public event PropertyChangedEventHandler? PropertyChanged;


        public PlotModel ForcePlot { get; private set; }
        public PlotModel LengthPlot { get; private set; }
        public PlotModel ForceLengthPlot { get; private set; }
        public List<string> PortList { get; set; }
        public List<int> RS485Address { get; set; }
        public static Array SpecimenTypeValues => Enum.GetValues(typeof(SpecimenType));
        public static List<int> BaudRatesValues => Models.SerialPortCommunications.BaudRates();

        public void UpdatePortList()
        {
            this.PortList = SerialPort.GetPortNames().ToList<string>();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PortList)));
        }

    }
}