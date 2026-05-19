using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.IO;
using System.Windows;
using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Abstractions;
using TensileTestingApp.Services.Implementations;
using TensileTestingApp.Views;

namespace TensileTestingApp;
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        private static readonly string CrashLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TensileTestingApp", "crash.log");

        public App()
        {
            InitializeComponent();

            // Layer 1: WPF UI thread exceptions — app can continue
            DispatcherUnhandledException += (s, e) =>
            {
                LogCrash("DispatcherUnhandled", e.Exception);

                var result = ShowErrorDialog(
                    "Unexpected Error",
                    "An unexpected error occurred. You may continue working or close the application.",
                    e.Exception,
                    showContinue: true);

                e.Handled = result == MessageBoxResult.Yes; // Yes = Continue
            };

            // Layer 2: CLR unhandled exceptions on background threads — app is terminating
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogCrash("DomainUnhandled", ex);

                Dispatcher.InvokeAsync(() =>
                    ShowErrorDialog(
                        "Fatal Error",
                        "A fatal error occurred. The application will now close.",
                        ex,
                        showContinue: false));
            };

            // Layer 3: Unobserved Task exceptions — mark observed, log silently
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogCrash("UnobservedTask", e.Exception);
                e.SetObserved();
            };
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var settings = LoadSettings();
            services.AddSingleton(settings);

            services.AddSingleton<IZeroCorrectionService>(sp =>
                new ZeroCorrectionService(sp.GetRequiredService<AppSettings>().ZeroCorrection));
            services.AddSingleton<IPreloadService>(sp =>
                new PreloadService(sp.GetRequiredService<AppSettings>().Preload));

            services.AddSingleton<Services.PdfExportService>(sp =>
                new Services.PdfExportService(new AppLogger(sp.GetRequiredService<AppSettings>().Logging)));

            services.AddTransient<ViewModel.MainWindowViewModel>();
            services.AddTransient<MainPage>();
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            (_serviceProvider as IDisposable)?.Dispose();
            base.OnExit(e);
        }

        private static AppSettings LoadSettings()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            return config.Get<AppSettings>() ?? new AppSettings();
        }

        private static MessageBoxResult ShowErrorDialog(
            string title, string message, Exception? ex, bool showContinue)
        {
            var details = ex is not null
                ? $"\n\nDetails:\n{ex.GetType().Name}: {ex.Message}"
                : string.Empty;

            var footer = showContinue
                ? "\n\nClick Yes to continue, No to close the application."
                : $"\n\nCrash log: {CrashLogPath}";

            var buttons = showContinue ? MessageBoxButton.YesNo : MessageBoxButton.OK;
            var icon = showContinue ? MessageBoxImage.Warning : MessageBoxImage.Error;

            return MessageBox.Show(
                message + details + footer,
                title,
                buttons,
                icon);
        }

        private static void LogCrash(string source, Exception? ex)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CrashLogPath)!);
                var msg = $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{source}]\n{ex}\n";
                File.AppendAllText(CrashLogPath, msg);
            }
            catch (Exception logEx)
            {
                Debug.WriteLine($"[CRASH-LOG-FAIL:{source}] {logEx.Message}");
            }

            Debug.WriteLine($"[CRASH:{source}] {ex?.Message}");
        }
    }
