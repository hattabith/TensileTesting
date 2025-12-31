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
        public static Array SpecimenTypeValues => Enum.GetValues(typeof(SpecimentType));
        public static List<string> BaudRatesValues => Models.SerialPortCommunications.BaudRates();

        // TODO: Implement DCON command list and functionality
        // TODO: Add command format to documentation
        // TODO: Thinking aabout XML documentation and use DocFX to generate documentation

        /* Command format
         *  -----------------------------------------------------------
         * | Leading Character | Module Address | Data | [CHKSUM] | CR |
         * ------------------------------------------------------------
         * 
         * Most important commands:
         * #AA - Read the analog input for all channels
         * -- Valid response: >(Data)[CHKSUM](CR)
         * -- Example: >+025.12+020.45+012.78+018.97+003.24+015.35+008.07+014.79
         * #AAN - Read the analog input for channel N (N = 0 - 7)
         * -- Valid response: >(Data)[CHKSUM](CR)
         * -- Example: >+025.12
         * $AAA - Read the analog input for all channels in HEX format
         * -- Valid response: >(Data)[CHKSUM](CR)
         * -- Example:  >0000012301257FFF1802744F98238124
         * $AAF - Read the firmware version
         * -- Valid response: !AA(Data)[CHKSUM](CR)
         * -- Example:  !01A2.0
         * $AAM - Read the module name
         * -- Valid response: !AA(Name)[CHKSUM](CR)
         * -- Example:  !017017
         * $AAP - Read the protocol
         * -- Valid response: !AASC[CHKSUM](CR)
         * ---- AA - Module address
         * ---- S - Protocol type (0 = Only DCON, 1 = DCON and Modbus RTU)
         * ---- C - Current protocol saved in EPROM (0 = DCON, 1 = Modbus RTU)
         * -- Example:  !0110
         * @AAS - Read the differential/single-ended connection mode status
         * -- Valid response: !AAN(MODE)[CHKSUM](CR)
         * ---- N - Current connection mode (0 = Differential, 1 = Single-ended)
         * -- Example:  !010
         * ~** - Informs all modules that the host is OK
         * 
         * 
         * # - ChannelsReadDelimiter
         * $ - SystemQueryDelimiter
         * @ - ConfigStatusDelimiter
         * ~ - BroadCastDelimiter
         * 
         * 
         */

        public void UpdatePortList()
        {
            this.PortList = SerialPort.GetPortNames().ToList<string>();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PortList)));
        }


    }
}