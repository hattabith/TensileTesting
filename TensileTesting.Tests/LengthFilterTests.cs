using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests
{
    /// <summary>
    /// Unit tests for the Length channel filter pipeline.
    /// Validates that SG filter behaves correctly on length-like values (0–300 mm range).
    /// </summary>
    public class LengthFilterTests
    {
        // ── Default configuration ──────────────────────────────────────────────

        [Fact]
        public void DefaultLengthFilterSettings_HasFilterEnabled()
        {
            var settings = new FilterSettings();
            Assert.True(settings.EnableForceFilter);
        }

        [Fact]
        public void DefaultLengthFilterSettings_TypeIsSG()
        {
            var settings = new FilterSettings();
            Assert.Equal("SG", settings.Type, ignoreCase: true);
        }

        [Fact]
        public void DefaultLengthFilterSettings_WindowIs7()
        {
            var settings = new FilterSettings();
            Assert.Equal(7, settings.SavitzkyGolayWindow);
        }

        [Fact]
        public void AppSettings_HasSeparateLengthFilterSection()
        {
            var appSettings = new AppSettings();
            Assert.NotNull(appSettings.LengthFilter);
            // Force and Length filters are independent instances
            Assert.NotSame(appSettings.Filter, appSettings.LengthFilter);
        }

        // ── Filter applied to length-like values ──────────────────────────────

        [Fact]
        public void Filter_ConstantLengthSignal_RemainsConstant()
        {
            var filter = new SavitzkyGolayFilter(7);

            for (int i = 0; i < 20; i++)
                filter.Filter(150.0); // mid-range length in mm

            double result = filter.Filter(150.0);
            Assert.Equal(150.0, result, precision: 10);
        }

        [Fact]
        public void Filter_SlowRisingLength_SmoothsCorrectly()
        {
            var filter = new SavitzkyGolayFilter(7);

            // Simulate slow linear extension: 0..14 mm
            for (int i = 0; i < 14; i++)
                filter.Filter(i * 1.0);

            double result = filter.Filter(14.0);
            // SG preserves linear trends exactly — result should be close to 14.0
            Assert.InRange(result, 10.0, 14.0);
        }

        [Fact]
        public void Filter_ZeroLength_ReturnsZero()
        {
            var filter = new SavitzkyGolayFilter(5);

            for (int i = 0; i < 10; i++)
                filter.Filter(0.0);

            Assert.Equal(0.0, filter.Filter(0.0), precision: 10);
        }

        [Fact]
        public void Filter_WarmupPhase_ReturnsRunningAverage()
        {
            var filter = new SavitzkyGolayFilter(5);

            // First 4 calls are warm-up (window=5, need 5 samples for full window)
            Assert.Equal(100.0, filter.Filter(100.0), precision: 10);
            Assert.Equal(110.0, filter.Filter(120.0), precision: 10); // avg(100,120)
            Assert.Equal(120.0, filter.Filter(140.0), precision: 10); // avg(100,120,140)
            Assert.Equal(130.0, filter.Filter(160.0), precision: 10); // avg(100,120,140,160)
        }

        [Fact]
        public void Filter_Reset_ClearsBufferAndRestartsWarmup()
        {
            var filter = new SavitzkyGolayFilter(7);

            for (int i = 0; i < 10; i++)
                filter.Filter(200.0);

            filter.Reset();

            // After reset, first call is warm-up again — returns the single value
            double firstAfterReset = filter.Filter(50.0);
            Assert.Equal(50.0, firstAfterReset, precision: 10);
        }

        [Fact]
        public void Filter_ForceAndLengthFilters_AreIndependent()
        {
            // Force filter fed with large values, Length filter fed with small values
            var forceFilter = new SavitzkyGolayFilter(7);
            var lengthFilter = new SavitzkyGolayFilter(7);

            for (int i = 0; i < 10; i++)
            {
                forceFilter.Filter(50.0);  // kN range
                lengthFilter.Filter(1.0);  // mm range
            }

            double forceResult = forceFilter.Filter(50.0);
            double lengthResult = lengthFilter.Filter(1.0);

            Assert.Equal(50.0, forceResult, precision: 10);
            Assert.Equal(1.0, lengthResult, precision: 10);
            Assert.NotEqual(forceResult, lengthResult);
        }

        [Fact]
        public void Filter_Window5_FullWindowCoefficients_LengthValues()
        {
            var filter = new SavitzkyGolayFilter(5);

            filter.Filter(10.0);
            filter.Filter(20.0);
            filter.Filter(30.0);
            filter.Filter(40.0);
            double result = filter.Filter(50.0);

            // Window 5 coefficients: [-3, 12, 17, 12, -3] / 35
            double expected = (-3.0 * 10.0 + 12.0 * 20.0 + 17.0 * 30.0 + 12.0 * 40.0 - 3.0 * 50.0) / 35.0;
            Assert.Equal(expected, result, precision: 10);
        }
    }
}
