using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows.Controls;
using TensileTestingApp.Models;

namespace TensileTestingApp.ViewModel
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        private Frame _mainWindowFrame;
        private readonly LineSeries _forceTimeSeries;
        private readonly LineSeries _rawForceTimeSeries;
        private readonly LineSeries _lengthTimeSeries;
        private readonly LineSeries _rawLengthTimeSeries;
        private readonly LineSeries _forceLengthSeries;


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
                Title = "Force vs Time"
            };
            this.ForcePlot.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time", StringFormat = "HH:mm:ss" });
            this.ForcePlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Force (kN)" });
            _rawForceTimeSeries = new LineSeries
            {
                Title = "Raw Force",
                Color = OxyColor.FromAColor(80, OxyColors.Red),
                StrokeThickness = 1.0,
                LineStyle = LineStyle.Solid
            };
            _forceTimeSeries = new LineSeries
            {
                Title = "Filtered Force",
                Color = OxyColors.Red,
                StrokeThickness = 2.0
            };
            this.ForcePlot.Series.Add(_rawForceTimeSeries);
            this.ForcePlot.Series.Add(_forceTimeSeries);

            this.LengthPlot = new PlotModel { Title = "Length vs Time" };
            this.LengthPlot.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, Title = "Time", StringFormat = "HH:mm:ss" });
            this.LengthPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Length (mm)" });
            _rawLengthTimeSeries = new LineSeries
            {
                Title = "Raw Length",
                Color = OxyColor.FromAColor(80, OxyColors.DodgerBlue),
                StrokeThickness = 1.0,
                LineStyle = LineStyle.Solid
            };
            _lengthTimeSeries = new LineSeries
            {
                Title = "Filtered Length",
                Color = OxyColors.DodgerBlue,
                StrokeThickness = 2.0
            };
            this.LengthPlot.Series.Add(_rawLengthTimeSeries);
            this.LengthPlot.Series.Add(_lengthTimeSeries);

            this.ForceLengthPlot = new PlotModel { Title = "Force vs Length" };
            this.ForceLengthPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Length (mm)" });
            this.ForceLengthPlot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Force (kN)" });
            _forceLengthSeries = new LineSeries { Title = "Filtered Force-Length", Color = OxyColors.ForestGreen };
            this.ForceLengthPlot.Series.Add(_forceLengthSeries);

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
        public static List<int> BaudRatesValues => new() { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

        public void UpdatePortList()
        {
            this.PortList = SerialPort.GetPortNames().ToList<string>();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PortList)));
        }

        public void ResetLiveData()
        {
            _rawForceTimeSeries.Points.Clear();
            _forceTimeSeries.Points.Clear();
            _rawLengthTimeSeries.Points.Clear();
            _lengthTimeSeries.Points.Clear();
            _forceLengthSeries.Points.Clear();
            ForcePlot.InvalidatePlot(true);
            LengthPlot.InvalidatePlot(true);
            ForceLengthPlot.InvalidatePlot(true);
        }

        public void AddLiveDataPoint(TensileTestData data)
        {
            double timeX = DateTimeAxis.ToDouble(data.Timestamp);
            _rawForceTimeSeries.Points.Add(new DataPoint(timeX, data.Force));
            _forceTimeSeries.Points.Add(new DataPoint(timeX, data.FilteredForce));
            _rawLengthTimeSeries.Points.Add(new DataPoint(timeX, data.Length));
            _lengthTimeSeries.Points.Add(new DataPoint(timeX, data.FilteredLength));
            _forceLengthSeries.Points.Add(new DataPoint(data.FilteredLength, data.FilteredForce));

            ForcePlot.InvalidatePlot(true);
            LengthPlot.InvalidatePlot(true);
            ForceLengthPlot.InvalidatePlot(true);
        }

    }
}