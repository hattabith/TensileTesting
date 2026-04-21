using System.Diagnostics;
using System.IO;
using System.Text;
using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations;
    public class AppLogger : ILogger
    {
        private readonly LoggingSettings _settings;
        private readonly object _sync = new();

        public AppLogger()
            : this(new LoggingSettings())
        {
        }

        public AppLogger(LoggingSettings settings)
        {
            _settings = settings;
            EnsureLogDirectory();
            CleanupOldLogFiles();
        }

        public void LogInfo(string message)
        {
            if (string.Equals(_settings.MinimumLevel, "Error", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Write("INFO", message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            string fullMessage = exception is null ? message : $"{message}. Exception: {exception}";
            Write("ERROR", fullMessage);
        }

        private void Write(string level, string message)
        {
            string line = $"[{level}] {DateTime.Now:O} {message}";

            if (_settings.EnableDebugOutput)
            {
                Debug.WriteLine(line);
            }

            if (!string.Equals(_settings.Provider, "File", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lock (_sync)
            {
                string filePath = BuildLogFilePath();
                File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        private string BuildLogFilePath()
        {
            string folder = Environment.ExpandEnvironmentVariables(_settings.File.Folder);
            string fileName = _settings.File.FileNamePattern.Replace("{Date}", DateTime.Now.ToString("yyyy-MM-dd"));
            return Path.Combine(folder, fileName);
        }

        private void EnsureLogDirectory()
        {
            if (!string.Equals(_settings.Provider, "File", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string folder = Environment.ExpandEnvironmentVariables(_settings.File.Folder);
            Directory.CreateDirectory(folder);
        }

        private void CleanupOldLogFiles()
        {
            if (!string.Equals(_settings.Provider, "File", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string folder = Environment.ExpandEnvironmentVariables(_settings.File.Folder);
            if (!Directory.Exists(folder))
            {
                return;
            }

            int keep = Math.Max(1, _settings.File.RetainedFileCountLimit);
            string prefix = _settings.File.FileNamePattern.Split("{Date}", StringSplitOptions.None)[0];
            string suffix = _settings.File.FileNamePattern.Split("{Date}", StringSplitOptions.None).LastOrDefault() ?? string.Empty;

            var files = Directory.GetFiles(folder)
                .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                               && Path.GetFileName(path).EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetCreationTimeUtc)
                .ToList();

            foreach (string file in files.Skip(keep))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                }
            }
        }
    }
