using System.Globalization;
namespace TensileTesting.Tests
{
    public class ADCParseUnitTests
    {
        private CultureInfo _culture = CultureInfo.InvariantCulture;
        private CultureInfo _cultureUI = CultureInfo.InvariantCulture;
        [Fact]
        public void Test1()
        {
            Assert.True(true);
        }
        [Theory]
        [InlineData("11/02/2026 20:00:58 >-07.200+00.050-00.000-00.000-00.000-00.000-00.000-00.00098", "11/02/2026 20:00:58", -7.2d, 0.05d)]
        [InlineData("01/02/2025 02:23:46 >+23.070-03.260-00.000-00.000-00.000-00.000-00.000-00.00098", "01/02/2025 02:23:46", 23.07d, -3.26d)]
        public void TestADCDataParserCheckSum(string data, string expectedDateTime, double expectedForce, double expectedLength)  // TODO: Didvide into different tests
        {
            // TODO: Check CheckSum calculation
            Thread.CurrentThread.CurrentCulture = _culture;
            Thread.CurrentThread.CurrentUICulture = _cultureUI;
            var parser = new TensileTestingApp.Models.ADCDataParser();
            var result = parser.ParseWithCheckSum(data);
            Assert.Equal(result.Timestamp.ToString(), expectedDateTime);
            Assert.Equal(result.Force, expectedForce);
            Assert.Equal(result.Length, expectedLength);
        }
        [Theory]
        [InlineData("11/02/2026 20:00:58 >-07.200+00.050-00.000-00.000-00.000-00.000-00.000-00.000", "11/02/2026 20:00:58", -7.2d, 0.05d)]
        [InlineData("01/02/2025 02:23:46 >+23.070-03.260-00.000-00.000-00.000-00.000-00.000-00.000", "01/02/2025 02:23:46", 23.07d, -3.26d)]
        public void TestADCParserNoCheckSum(string data, string expectedDateTime, double expectedForce, double expectedLength)
        {
            Thread.CurrentThread.CurrentCulture = _culture;
            Thread.CurrentThread.CurrentUICulture = _cultureUI;
            var parser = new TensileTestingApp.Models.ADCDataParser();
            var result = parser.ParseWithOutCheckSum(data);
            Assert.Equal(result.Timestamp.ToString(), expectedDateTime);
            Assert.Equal(result.Force, expectedForce);
            Assert.Equal(result.Length, expectedLength);
        }
    }
}
