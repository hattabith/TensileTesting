using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests
{
    public class ExponentialMovingAverageFilterTests
    {
        // ── Constructor validation ────────────────────────────────────────────

        [Theory]
        [InlineData(0.0)]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(double.NegativeInfinity)]
        public void Constructor_InvalidAlpha_ThrowsArgumentOutOfRangeException(double alpha)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ExponentialMovingAverageFilter(alpha));
        }

        [Theory]
        [InlineData(0.0001)]
        [InlineData(0.5)]
        [InlineData(1.0)]
        public void Constructor_ValidAlpha_DoesNotThrow(double alpha)
        {
            var ex = Record.Exception(() => new ExponentialMovingAverageFilter(alpha));
            Assert.Null(ex);
        }

        // ── First-sample passthrough (initialisation behaviour) ───────────────

        [Fact]
        public void Filter_FirstSample_ReturnsInputUnchanged()
        {
            var filter = new ExponentialMovingAverageFilter(0.5);
            Assert.Equal(10.0, filter.Filter(10.0));
        }

        [Fact]
        public void Filter_FirstSampleNegative_ReturnsInputUnchanged()
        {
            var filter = new ExponentialMovingAverageFilter(0.5);
            Assert.Equal(-42.5, filter.Filter(-42.5));
        }

        // ── Alpha = 1 is a transparent passthrough ────────────────────────────

        [Fact]
        public void Filter_AlphaOne_IsTransparentPassthrough()
        {
            var filter = new ExponentialMovingAverageFilter(1.0);
            Assert.Equal(5.0, filter.Filter(5.0));
            Assert.Equal(7.0, filter.Filter(7.0));
            Assert.Equal(3.0, filter.Filter(3.0));
        }

        // ── Smoothing formula  y[n] = α·x[n] + (1−α)·y[n−1] ─────────────────

        [Fact]
        public void Filter_AlphaHalf_SecondSampleIsHalfwayBetween()
        {
            var filter = new ExponentialMovingAverageFilter(0.5);
            filter.Filter(10.0);           // y[0] = 10
            double result = filter.Filter(0.0);  // y[1] = 0.5·0 + 0.5·10 = 5
            Assert.Equal(5.0, result, precision: 10);
        }

        [Fact]
        public void Filter_SmallAlpha_LargeStepProducesSmallResponse()
        {
            var filter = new ExponentialMovingAverageFilter(0.1);
            filter.Filter(0.0);            // y[0] = 0
            double result = filter.Filter(100.0);  // y[1] = 0.1·100 + 0.9·0 = 10
            Assert.Equal(10.0, result, precision: 10);
        }

        [Fact]
        public void Filter_ConstantInput_OutputEqualsInput()
        {
            var filter = new ExponentialMovingAverageFilter(0.3);
            for (int i = 0; i < 50; i++)
                filter.Filter(7.0);
            Assert.Equal(7.0, filter.Filter(7.0), precision: 10);
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Fact]
        public void Reset_AfterHistory_FirstSampleBehavesAsNewInit()
        {
            var filter = new ExponentialMovingAverageFilter(0.1);
            filter.Filter(100.0);   // load up history
            filter.Filter(100.0);
            filter.Reset();

            // After reset the first sample must be returned unchanged
            Assert.Equal(5.0, filter.Filter(5.0));
        }

        [Fact]
        public void Reset_ClearsState_SecondSampleAfterResetUsesNewHistory()
        {
            var filter = new ExponentialMovingAverageFilter(0.5);
            filter.Filter(100.0);
            filter.Reset();
            filter.Filter(0.0);             // y[0] = 0 (new init)
            double result = filter.Filter(10.0);   // y[1] = 0.5·10 + 0.5·0 = 5
            Assert.Equal(5.0, result, precision: 10);
        }
    }
}
