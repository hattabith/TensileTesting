using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests
{
    public class SavitzkyGolayFilterTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(6)]
        [InlineData(9)]
        public void Constructor_UnsupportedWindow_ThrowsArgumentOutOfRangeException(int windowSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new SavitzkyGolayFilter(windowSize));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(7)]
        public void Constructor_SupportedWindow_DoesNotThrow(int windowSize)
        {
            var ex = Record.Exception(() => new SavitzkyGolayFilter(windowSize));
            Assert.Null(ex);
        }

        [Fact]
        public void WindowSize_ReturnsConfiguredValue()
        {
            var filter = new SavitzkyGolayFilter(7);
            Assert.Equal(7, filter.WindowSize);
        }

        [Fact]
        public void Filter_WarmupBeforeFullWindow_ReturnsSimpleAverage()
        {
            var filter = new SavitzkyGolayFilter(5);

            Assert.Equal(10.0, filter.Filter(10.0), precision: 10);
            Assert.Equal(15.0, filter.Filter(20.0), precision: 10);
            Assert.Equal(20.0, filter.Filter(30.0), precision: 10);
            Assert.Equal(25.0, filter.Filter(40.0), precision: 10);
        }

        [Fact]
        public void Filter_FullWindow_AppliesSavitzkyGolayCoefficients_ForWindow5()
        {
            var filter = new SavitzkyGolayFilter(5);

            filter.Filter(1.0);
            filter.Filter(2.0);
            filter.Filter(3.0);
            filter.Filter(4.0);
            double result = filter.Filter(5.0);

            // Window 5 coefficients: [-3, 12, 17, 12, -3] / 35
            double expected = (-3.0 * 1.0 + 12.0 * 2.0 + 17.0 * 3.0 + 12.0 * 4.0 - 3.0 * 5.0) / 35.0;
            Assert.Equal(expected, result, precision: 10);
        }

        [Fact]
        public void Filter_ConstantSignal_RemainsConstant()
        {
            var filter = new SavitzkyGolayFilter(7);

            for (int i = 0; i < 20; i++)
                filter.Filter(3.5);

            Assert.Equal(3.5, filter.Filter(3.5), precision: 10);
        }

        [Fact]
        public void Filter_StepChange_ShowsExpectedLaggedResponse()
        {
            var filter = new SavitzkyGolayFilter(7);

            for (int i = 0; i < 7; i++)
                filter.Filter(0.0);

            // After first non-zero sample, trailing SG should not jump directly to 10.
            double first = filter.Filter(10.0);
            Assert.InRange(first, -5.0, 10.0);

            // After additional samples at 10, output should converge toward 10.
            double second = filter.Filter(10.0);
            double third = filter.Filter(10.0);
            double fourth = filter.Filter(10.0);
            Assert.True(fourth >= third && third >= second);
        }

        [Fact]
        public void Reset_ClearsStateAndRestartsWarmup()
        {
            var filter = new SavitzkyGolayFilter(7);
            filter.Filter(10.0);
            filter.Filter(20.0);
            filter.Reset();

            Assert.Equal(5.0, filter.Filter(5.0), precision: 10);
            Assert.Equal(7.5, filter.Filter(10.0), precision: 10);
        }
    }
}
