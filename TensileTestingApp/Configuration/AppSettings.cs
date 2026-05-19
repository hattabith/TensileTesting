namespace TensileTestingApp.Configuration;
    public class AppSettings
    {
        public SerialPortSettings SerialPort { get; set; } = new();
        public AcquisitionSettings Acquisition { get; set; } = new();
        public ParserSettings Parser { get; set; } = new();
        public RecordingSettings Recording { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
        public UiSettings Ui { get; set; } = new();
        public FilterSettings Filter { get; set; } = new();
        public FilterSettings LengthFilter { get; set; } = new();
        public ZeroCorrectionSettings ZeroCorrection { get; set; } = new();
        public PreloadSettings Preload { get; set; } = new();
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
        public int BatchSizePoints { get; set; } = 10;
        public string Header { get; set; } = "DateTime;RawForce;FilteredForce;CorrectedForce;PreloadAdjustedForce;RawLength;FilteredLength;CorrectedLength;PreloadAdjustedLength";
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

    public class ZeroCorrectionSettings
    {
        /// <summary>Number of filtered samples to collect during a zero-capture window.</summary>
        public int CaptureSamples { get; set; } = 40;
        /// <summary>Fraction of outlier samples trimmed from each end when computing the baseline mean (0 to 0.4).</summary>
        public double TrimFraction { get; set; } = 0.10;
        /// <summary>Maximum RMS noise (kN) classified as Good quality baseline.</summary>
        public double GoodNoiseThresholdKn { get; set; } = 0.05;
        /// <summary>Maximum RMS noise (kN) still accepted as a usable baseline (above = Bad quality).</summary>
        public double MaxNoiseThresholdKn { get; set; } = 0.20;
        /// <summary>Keep zero offsets across Start/Stop recording cycles within the same session.</summary>
        public bool PreserveAcrossRecording { get; set; } = true;
        /// <summary>Clear zero offsets when the serial port is disconnected.</summary>
        public bool ClearOnDisconnect { get; set; } = false;
    }

    public class PreloadSettings
    {
        /// <summary>Preload mode: "OffsetSubtraction" or "OriginShift".</summary>
        public string Mode { get; set; } = "OffsetSubtraction";
        /// <summary>Force threshold (kN) above which preload is considered reached.</summary>
        public double ThresholdKn { get; set; } = 0.5;
        /// <summary>Hysteresis band (kN) – threshold must exceed this margin to lock.</summary>
        public double HysteresisKn { get; set; } = 0.05;
    }

    public class FilterSettings
    {
        /// <summary>Enable real-time smoothing of the Force channel.</summary>
        public bool EnableForceFilter { get; set; } = true;

        /// <summary>Filter algorithm: "SG" (Savitzky-Golay), "EMA" or "MA".</summary>
        public string Type { get; set; } = "SG";

        /// <summary>EMA smoothing factor α ∈ (0, 1]. Lower = more smoothing. Default 0.1.</summary>
        public double EmaAlpha { get; set; } = 0.1;

        /// <summary>Window size for Moving Average filter (number of samples). Default 10.</summary>
        public int MovingAverageWindow { get; set; } = 10;

        /// <summary>Window size for Savitzky-Golay smoothing. Supported values: 5 or 7.</summary>
        public int SavitzkyGolayWindow { get; set; } = 7;
    }
