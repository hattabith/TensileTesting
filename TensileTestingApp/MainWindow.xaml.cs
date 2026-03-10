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
            var page = new Views.MainPage(settings)
            {
                DataContext = vm
            };
            vm.CurrentPage = page;
            this.DataContext = vm;


        }
    }
}