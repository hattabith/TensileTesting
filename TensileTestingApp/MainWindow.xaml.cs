using System.Windows;
namespace TensileTestingApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {

            InitializeComponent();

            var vm = new ViewModel.MainWindowViewModel();
            vm.CurrentPage = new Views.MainPage();
            this.DataContext = vm;

        }
    }
}