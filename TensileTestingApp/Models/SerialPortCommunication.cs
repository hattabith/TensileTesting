using System.IO.Ports;

namespace TensileTestingApp.Models
{
    public class SerialPortCommunication
    {
        private SerialPort port;
        private string _portName;
        private int _baudRate;
        private int _deviceAddress;
        public SerialPortCommunication(string PortName, int BaudRate, int DeviceAddress)
        {
            _portName = PortName;
            _baudRate = BaudRate;
            _deviceAddress = DeviceAddress;

            port = new SerialPort();
            port.PortName = PortName;
            port.BaudRate = BaudRate;
        }
        public string GetPortName()
        {
            return _portName.ToString();
        }
        public int GetBaudRate()
        {
            return _baudRate;
        }
        public int GetDeviceAddress()
        {
            return _deviceAddress;
        }
        public bool IsOpen
        {
            get => port.IsOpen;
        }
        public void OpenConnection()
        {
            if (!port.IsOpen)
            {
                try
                {
                    port.Open();
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to open serial port: " + ex.Message);
                }
            }
        }
        public void CloseConnection()
        {
            if (port.IsOpen)
            {
                try
                {
                    port.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to close serial port: " + ex.Message);
                }
            }
        }
        public void WriteToPort(string command)
        {
            if (!port.IsOpen)
            {
                throw new Exception("Serial port is not open.");
            }
            try
            {
                port.WriteLine(command);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during communication: " + ex.Message);
            }
        }
        public string ReadFromPort(int timeout)
        {
            try
            {
                port.ReadTimeout = timeout;
                return port.ReadLine();
            }
            catch (Exception ex)
            {
                throw new Exception("Serial port timeout error" + ex.Message);
            }
        }
    }
}
