namespace TensileTesting.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Assert.True(true);
        }
        [Theory]
        [InlineData("11.02.2026 20:00:58 >-07.200+00.050-00.000-00.000-00.000-00.000-00.000-00.00098", "02.11.2026 20:00:58", -7.2, +0.05)]
        [InlineData("01.02.2025 02:23:46 >+23.070-03.260-00.000-00.000-00.000-00.000-00.000-00.00098", "02.01.2025 2:23:46", 23.07, -3.26)]
        public void TestADCDataParser(string data, string expectedDateTime, double expectedForce, double expectedLength)
        {
            var parser = new TensileTestingApp.Models.ADCDataParser();
            var result = parser.ParseWithOutCheckSum(data);
            Assert.Equal(result.Timestamp.ToString(), expectedDateTime);
            Assert.Equal(result.Force, expectedForce);
            Assert.Equal(result.Length, expectedLength);

        }
    }
}
