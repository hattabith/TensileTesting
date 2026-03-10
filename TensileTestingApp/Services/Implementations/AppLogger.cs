using System.Diagnostics;
using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations
{
    public class AppLogger : ILogger
    {
        public void LogInfo(string message)
        {
            Debug.WriteLine($"[INFO] {DateTime.Now:O} {message}");
        }

        public void LogError(string message, Exception? exception = null)
        {
            if (exception is null)
            {
                Debug.WriteLine($"[ERROR] {DateTime.Now:O} {message}");
                return;
            }

            Debug.WriteLine($"[ERROR] {DateTime.Now:O} {message}. Exception: {exception}");
        }
    }
}
