using OxyPlot;
using OxyPlot.Series;
using System.ComponentModel;
using System.Windows.Controls;

namespace TensileTestingApp.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
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
            this.MyModel = new PlotModel { Title = "Tensile Test Data" };
            this.MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));

        }
        public event PropertyChangedEventHandler? PropertyChanged;


        public PlotModel MyModel { get; private set; }

    }
}