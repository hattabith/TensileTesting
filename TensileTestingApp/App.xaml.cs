using System.Windows;
using Microsoft.Extensions.Configuration;
using TensileTestingApp.Configuration;


namespace TensileTestingApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppSettings Settings { get; private set; } = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Settings = LoadSettings();

            var mainWindow = new MainWindow(Settings);
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private static AppSettings LoadSettings()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            return config.Get<AppSettings>() ?? new AppSettings();
        }
    }

}
