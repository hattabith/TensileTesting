using System.IO.Ports;

namespace TensileTestingApp.Models
{
    public class SerialPortCommunications
    {
        private SerialPort port;
        private string _portName;
        private int _baudRate;
        private int _deviceAddress;
        public SerialPortCommunications(string PortName, int BaudRate, int DeviceAddress)
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
        public List<string> GetAvailablePorts()
        {
            return SerialPort.GetPortNames().ToList<string>();
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
        public void WriteToPort(string command, int timeout)
        {
            if (!port.IsOpen)
            {
                throw new Exception("Serial port is not open.");
            }
            try
            {
                port.WriteTimeout = timeout;
                port.WriteLine(command);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during communication: " + ex.Message);
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
                return port.ReadLine().ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Serial port timeout error" + ex.Message);
            }
        }
        public string ReadFromPort()
        {
            try
            {
                return port.ReadLine();  // Check if ToString() is necessary
                // TODO: Implement async read with event handler for better performance
            }
            catch (Exception ex)
            {
                throw new Exception("Serial port timeout error" + ex.Message);
            }
        }
        public static List<int> BaudRates()
        {
            return new List<int> { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        }
    }
}
