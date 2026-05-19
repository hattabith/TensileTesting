using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests
{
    public class SerialPortServiceTests
    {
        [Fact]
        public void Configure_SetsPortBaudAndDeviceAddress()
        {
            var service = new SerialPortService();

            service.Configure("COM7", 19200, 3);

            Assert.Equal("COM7", service.PortName);
            Assert.Equal(19200, service.BaudRate);
            Assert.Equal(3, service.DeviceAddress);
        }

        [Fact]
        public void GetSupportedBaudRates_ReturnsExpectedSet()
        {
            var service = new SerialPortService();

            var rates = service.GetSupportedBaudRates();

            Assert.Equal(new[] { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 }, rates);
        }

        [Fact]
        public void GetAvailablePorts_DoesNotThrow()
        {
            var service = new SerialPortService();

            var ex = Record.Exception(() => service.GetAvailablePorts());

            Assert.Null(ex);
        }

        [Fact]
        public void CloseConnection_WhenAlreadyClosed_DoesNotThrow()
        {
            var service = new SerialPortService();

            var ex = Record.Exception(service.CloseConnection);

            Assert.Null(ex);
        }

        [Fact]
        public void OpenConnection_WithInvalidPort_ThrowsInvalidOperationException()
        {
            var service = new SerialPortService();
            service.Configure("INVALID_PORT", 9600, 1);

            Assert.Throws<InvalidOperationException>(service.OpenConnection);
        }

        [Fact]
        public void WriteToPort_WhenPortIsClosed_ThrowsInvalidOperationException()
        {
            var service = new SerialPortService();

            Assert.Throws<InvalidOperationException>(() => service.WriteToPort("#0184"));
        }

        [Fact]
        public void WriteToPortWithTimeout_WhenPortIsClosed_ThrowsInvalidOperationException()
        {
            var service = new SerialPortService();

            Assert.Throws<InvalidOperationException>(() => service.WriteToPort("#0184", 100));
        }

        [Fact]
        public void ReadFromPort_WhenPortIsClosed_ThrowsTimeoutException()
        {
            var service = new SerialPortService();

            Assert.Throws<TimeoutException>(() => service.ReadFromPort());
        }

        [Fact]
        public void ReadFromPortWithTimeout_WhenPortIsClosed_ThrowsTimeoutException()
        {
            var service = new SerialPortService();

            Assert.Throws<TimeoutException>(() => service.ReadFromPort(100));
        }
    }
}
