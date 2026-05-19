using System.IO.Ports;
using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations;
    public class SerialPortService : ISerialPortService
    {
        private readonly SerialPort _port = new();

        public string PortName { get; private set; } = string.Empty;
        public int BaudRate { get; private set; }
        public int DeviceAddress { get; private set; }

        public bool IsOpen => _port.IsOpen;

        public void Configure(string portName, int baudRate, int deviceAddress)
        {
            PortName = portName;
            BaudRate = baudRate;
            DeviceAddress = deviceAddress;

            _port.PortName = portName;
            _port.BaudRate = baudRate;
            _port.NewLine = "\r";
        }

        public List<string> GetAvailablePorts()
        {
            return SerialPort.GetPortNames().ToList();
        }

        public List<int> GetSupportedBaudRates()
        {
            return new List<int> { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
        }

        public void OpenConnection()
        {
            if (_port.IsOpen)
            {
                return;
            }

            try
            {
                _port.Open();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to open serial port.", ex);
            }
        }

        public async Task OpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_port.IsOpen)
            {
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _port.Open();
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to open serial port.", ex);
            }
        }

        public void CloseConnection()
        {
            if (!_port.IsOpen)
            {
                return;
            }

            try
            {
                _port.Close();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to close serial port.", ex);
            }
        }

        public void WriteToPort(string command, int timeout)
        {
            if (!_port.IsOpen)
            {
                throw new InvalidOperationException("Serial port is not open.");
            }

            try
            {
                _port.WriteTimeout = timeout;
                _port.WriteLine(command);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error during serial write.", ex);
            }
        }

        public void WriteToPort(string command)
        {
            if (!_port.IsOpen)
            {
                throw new InvalidOperationException("Serial port is not open.");
            }

            try
            {
                _port.WriteLine(command);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error during serial write.", ex);
            }
        }

        public string ReadFromPort(int timeout)
        {
            try
            {
                _port.ReadTimeout = timeout;
                return _port.ReadLine();
            }
            catch (Exception ex)
            {
                throw new TimeoutException("Serial port read timeout error.", ex);
            }
        }

        public string ReadFromPort()
        {
            try
            {
                return _port.ReadLine();
            }
            catch (Exception ex)
            {
                throw new TimeoutException("Serial port read timeout error.", ex);
            }
        }
    }
