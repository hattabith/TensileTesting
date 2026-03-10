namespace TensileTestingApp.Services.Abstractions
{
    public interface ISerialPortService
    {
        string PortName { get; }
        int BaudRate { get; }
        int DeviceAddress { get; }
        bool IsOpen { get; }

        void Configure(string portName, int baudRate, int deviceAddress);
        List<string> GetAvailablePorts();
        List<int> GetSupportedBaudRates();
        void OpenConnection();
        void CloseConnection();
        void WriteToPort(string command, int timeout);
        void WriteToPort(string command);
        string ReadFromPort(int timeout);
        string ReadFromPort();
    }
}
