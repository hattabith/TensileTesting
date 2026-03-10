namespace TensileTestingApp.Configuration
{
    public class AppSettings
    {
        public SerialPortSettings SerialPort { get; set; } = new();
        public AcquisitionSettings Acquisition { get; set; } = new();
        public ParserSettings Parser { get; set; } = new();
        public RecordingSettings Recording { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
        public UiSettings Ui { get; set; } = new();
    }

    public class SerialPortSettings
    {
        public string DefaultPortName { get; set; } = "COM3";
        public int DefaultBaudRate { get; set; } = 9600;
        public int DefaultDeviceAddress { get; set; } = 1;
        public int WriteTimeoutMs { get; set; } = 1000;
        public int ReadTimeoutMs { get; set; } = 300;
        public int PollingIntervalMs { get; set; } = 50;
        public bool UseChecksum { get; set; } = true;
    }

    public class AcquisitionSettings
    {
        public string Culture { get; set; } = "en-US";
        public bool AutoConnectOnStartup { get; set; }
        public int MaxUiBufferLines { get; set; } = 5000;
        public bool EnableRawFrameLog { get; set; } = true;
    }

    public class ParserSettings
    {
        public double ForceScale { get; set; } = -100.0;
        public double LengthScale { get; set; } = 10.0;
        public bool ReturnInvalidSentinel { get; set; } = true;
        public double InvalidForceValue { get; set; } = -1.0;
        public double InvalidLengthValue { get; set; } = -1.0;
    }

    public class RecordingSettings
    {
        public string BaseFolder { get; set; } = "%USERPROFILE%\\Documents\\TensileTests";
        public string FileEncoding { get; set; } = "utf-8";
        public string Delimiter { get; set; } = ";";
        public bool AutoFlush { get; set; } = true;
        public string Header { get; set; } = "DateTime;Force;Length";
        public int FlushIntervalMs { get; set; } = 100;
    }

    public class LoggingSettings
    {
        public string Provider { get; set; } = "File";
        public string MinimumLevel { get; set; } = "Information";
        public bool EnableDebugOutput { get; set; } = true;
        public LoggingFileSettings File { get; set; } = new();
    }

    public class LoggingFileSettings
    {
        public string Folder { get; set; } = "%LOCALAPPDATA%\\TensileTesting\\logs";
        public string FileNamePattern { get; set; } = "app-{Date}.log";
        public int RetainedFileCountLimit { get; set; } = 14;
        public int MaxFileSizeMb { get; set; } = 10;
    }

    public class UiSettings
    {
        public string DateTimeFormat { get; set; } = "O";
        public string ForceDisplayFormat { get; set; } = "F";
        public string LengthDisplayFormat { get; set; } = "F";
    }
}
