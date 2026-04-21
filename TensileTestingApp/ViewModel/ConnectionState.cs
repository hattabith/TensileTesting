namespace TensileTestingApp.ViewModel;
    public partial class MainWindowViewModel
    {
        public enum ConnectionState
        {
            Disconnected,
            Connecting,
            Initializing,
            Connected,
            Disconnecting,
            Error
        }


    }