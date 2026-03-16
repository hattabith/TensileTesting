using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests
{
    public class MovingAverageFilterTests
    {
        // ── Constructor validation ────────────────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Constructor_InvalidWindowSize_ThrowsArgumentOutOfRangeException(int windowSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovingAverageFilter(windowSize));
        }

        [Fact]
        public void Constructor_WindowSizeOne_DoesNotThrow()
        {
            var ex = Record.Exception(() => new MovingAverageFilter(1));
            Assert.Null(ex);
        }

        // ── WindowSize property ───────────────────────────────────────────────

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void WindowSize_ReturnsConfiguredValue(int size)
        {
            var filter = new MovingAverageFilter(size);
            Assert.Equal(size, filter.WindowSize);
        }

        // ── Window size 1 – passthrough ───────────────────────────────────────

        [Fact]
        public void Filter_WindowSizeOne_AlwaysReturnsLatestSample()
        {
            var filter = new MovingAverageFilter(1);
            Assert.Equal(5.0, filter.Filter(5.0));
            Assert.Equal(10.0, filter.Filter(10.0));
            Assert.Equal(-3.0, filter.Filter(-3.0));
        }

        // ── Warm-up period (buffer not yet full) ──────────────────────────────

        [Fact]
        public void Filter_DuringWarmup_AveragesOnlyAvailableSamples()
        {
            var filter = new MovingAverageFilter(3);

            Assert.Equal(2.0, filter.Filter(2.0), precision: 10);  // [2]     → 2
            Assert.Equal(3.0, filter.Filter(4.0), precision: 10);  // [2,4]   → 3
            Assert.Equal(4.0, filter.Filter(6.0), precision: 10);  // [2,4,6] → 4
        }

        // ── Sliding window after warm-up ──────────────────────────────────────

        [Fact]
        public void Filter_FullWindow_SlidesCorrectly()
        {
            var filter = new MovingAverageFilter(3);
            filter.Filter(2.0);
            filter.Filter(4.0);
            filter.Filter(6.0);  // window: [2,4,6] → avg = 4

            double result = filter.Filter(8.0);  // window: [4,6,8] → avg = 6
            Assert.Equal(6.0, result, precision: 10);
        }

        [Fact]
        public void Filter_ConstantInput_OutputEqualsInput()
        {
            var filter = new MovingAverageFilter(5);
            for (int i = 0; i < 20; i++)
                filter.Filter(3.0);
            Assert.Equal(3.0, filter.Filter(3.0), precision: 10);
        }

        [Fact]
        public void Filter_SuddenStepInput_OutputConvergesOverWindow()
        {
            var filter = new MovingAverageFilter(4);
            // Prime the window at 0
            filter.Filter(0.0);
            filter.Filter(0.0);
            filter.Filter(0.0);
            filter.Filter(0.0);

            // Step to 4 – output should climb by 1 each sample
            Assert.Equal(1.0, filter.Filter(4.0), precision: 10);  // [0,0,0,4] → 1
            Assert.Equal(2.0, filter.Filter(4.0), precision: 10);  // [0,0,4,4] → 2
            Assert.Equal(3.0, filter.Filter(4.0), precision: 10);  // [0,4,4,4] → 3
            Assert.Equal(4.0, filter.Filter(4.0), precision: 10);  // [4,4,4,4] → 4
        }

        // ── Reset ─────────────────────────────────────────────────────────────

        [Fact]
        public void Reset_ClearsBuffer_WarmupRestartsFromScratch()
        {
            var filter = new MovingAverageFilter(3);
            filter.Filter(10.0);
            filter.Filter(20.0);
            filter.Filter(30.0);
            filter.Reset();

            // After reset the first sample is the only one in the (empty) buffer
            Assert.Equal(5.0, filter.Filter(5.0), precision: 10);
        }

        [Fact]
        public void Reset_CalledMultipleTimes_DoesNotThrow()
        {
            var filter = new MovingAverageFilter(3);
            var ex = Record.Exception(() =>
            {
                filter.Reset();
                filter.Reset();
            });
            Assert.Null(ex);
        }
    }
}
