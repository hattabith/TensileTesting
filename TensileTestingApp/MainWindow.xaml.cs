using System.Windows;
using TensileTestingApp.Configuration;
namespace TensileTestingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
            : this(App.Settings)
        {
        }

        public MainWindow(AppSettings settings)
        {

            InitializeComponent();

            var vm = new ViewModel.MainWindowViewModel();
            vm.CurrentPage = new Views.MainPage(settings);
            this.DataContext = vm;


        }
    }
}