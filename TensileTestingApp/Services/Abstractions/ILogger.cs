namespace TensileTestingApp.Services.Abstractions
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message, Exception? exception = null);
    }
}
