using System.Windows;
using TensileTestingApp.ViewModel;
using TensileTestingApp.Views;

namespace TensileTestingApp;
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel, MainPage mainPage)
        {
            InitializeComponent();

            mainPage.DataContext = viewModel;
            viewModel.CurrentPage = mainPage;
            this.DataContext = viewModel;
        }
    }