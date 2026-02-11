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
        [InlineData("-03.700", -3.7)]
        [InlineData("+03.700", 3.7)]
        public void TestADCDataParser(string data, double expected)
        {
            var parser = new TensileTestingApp.Models.ADCDataParser();
            var result = parser.ParseWithOutCheckSum(data);
            Assert.NotNull(result);
        }
    }
}
