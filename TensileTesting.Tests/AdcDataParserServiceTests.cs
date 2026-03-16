using System.Globalization;
using TensileTestingApp.Configuration;
using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests
{
    public class AdcDataParserServiceTests
    {
        // Valid ISO-8601 frames matching the current regex pattern
        private const string ValidFrameNoChecksum =
            "2026-02-20T16:11:09.9247019+02:00 >+00.001+00.050-00.000-00.000-00.000-00.000-00.000-00.000";

        private const string ValidFrameWithChecksum =
            "2026-02-20T16:11:09.9247019+02:00 >+00.001+00.050-00.000-00.000-00.000-00.000-00.000-00.00093";

        // ── Timestamp parsing ────────────────────────────────────────────────

        [Fact]
        public void ParseWithoutChecksum_ValidFrame_ReturnsCorrectTimestamp()
        {
            var parser = new AdcDataParserService();
            var result = parser.ParseWithoutChecksum(ValidFrameNoChecksum);

            var expected = DateTime.Parse("2026-02-20T16:11:09.9247019+02:00", CultureInfo.InvariantCulture);
            Assert.Equal(expected, result.Timestamp);
        }

        [Fact]
        public void ParseWithChecksum_ValidFrame_ReturnsCorrectTimestamp()
        {
            var parser = new AdcDataParserService();
            var result = parser.ParseWithChecksum(ValidFrameWithChecksum);

            var expected = DateTime.Parse("2026-02-20T16:11:09.9247019+02:00", CultureInfo.InvariantCulture);
            Assert.Equal(expected, result.Timestamp);
        }

        // ── Force / Length scaling (default settings: ForceScale=-100, LengthScale=10) ──

        [Theory]
        [InlineData("2026-02-20T16:11:09.9247019+02:00 >+00.001+00.050-00.000-00.000-00.000-00.000-00.000-00.000", 0.001, 0.050)]
        [InlineData("2026-02-20T16:11:09.9247019+02:00 >-07.200+00.500-00.000-00.000-00.000-00.000-00.000-00.000", -7.200, 0.500)]
        [InlineData("2026-02-20T16:11:09.9247019+02:00 >+00.000-01.000-00.000-00.000-00.000-00.000-00.000-00.000", 0.000, -1.000)]
        public void ParseWithoutChecksum_ValidFrame_AppliesDefaultScaling(
            string frame, double rawCh0, double rawCh1)
        {
            var parser = new AdcDataParserService();
            var result = parser.ParseWithoutChecksum(frame);

            Assert.Equal(-100.0 * rawCh0, result.Force, precision: 5);
            Assert.Equal(10.0 * rawCh1, result.Length, precision: 5);
        }

        [Fact]
        public void ParseWithChecksum_ValidFrame_AppliesDefaultScaling()
        {
            var parser = new AdcDataParserService();
            var result = parser.ParseWithChecksum(ValidFrameWithChecksum);

            Assert.Equal(-100.0 * 0.001, result.Force, precision: 5);
            Assert.Equal(10.0 * 0.050, result.Length, precision: 5);
        }

        // ── Custom ParserSettings ─────────────────────────────────────────────

        [Fact]
        public void ParseWithoutChecksum_CustomScale_AppliesCustomScale()
        {
            var settings = new ParserSettings { ForceScale = -50.0, LengthScale = 5.0 };
            var parser = new AdcDataParserService(settings);
            var result = parser.ParseWithoutChecksum(ValidFrameNoChecksum);

            Assert.Equal(-50.0 * 0.001, result.Force, precision: 5);
            Assert.Equal(5.0 * 0.050, result.Length, precision: 5);
        }

        // ── Invalid / malformed frames ────────────────────────────────────────

        [Fact]
        public void ParseWithoutChecksum_InvalidFrame_ReturnsSentinelValues()
        {
            var parser = new AdcDataParserService();
            var result = parser.ParseWithoutChecksum("INVALID DATA");

            Assert.Equal(-1.0, result.Force);
            Assert.Equal(-1.0, result.Length);
        }

        [Fact]
        public void ParseWithChecksum_InvalidFrame_ReturnsSentinelValues()
        {
            var parser = new AdcDataParserService();
            var result = parser.ParseWithChecksum("INVALID DATA");

            Assert.Equal(-1.0, result.Force);
            Assert.Equal(-1.0, result.Length);
        }

        [Fact]
        public void ParseWithoutChecksum_EmptyString_ReturnsSentinelValues()
        {
            var parser = new AdcDataParserService();
            var result = parser.ParseWithoutChecksum(string.Empty);

            Assert.Equal(-1.0, result.Force);
            Assert.Equal(-1.0, result.Length);
        }

        [Fact]
        public void ParseWithoutChecksum_ReturnInvalidSentinelFalse_ReturnsZeroOnInvalid()
        {
            var settings = new ParserSettings { ReturnInvalidSentinel = false };
            var parser = new AdcDataParserService(settings);
            var result = parser.ParseWithoutChecksum("INVALID DATA");

            Assert.Equal(0.0, result.Force);
            Assert.Equal(0.0, result.Length);
        }

        [Fact]
        public void ParseWithChecksum_ReturnInvalidSentinelFalse_ReturnsZeroOnInvalid()
        {
            var settings = new ParserSettings { ReturnInvalidSentinel = false };
            var parser = new AdcDataParserService(settings);
            var result = parser.ParseWithChecksum("INVALID DATA");

            Assert.Equal(0.0, result.Force);
            Assert.Equal(0.0, result.Length);
        }

        // ── Frame without checksum must NOT be accepted by ParseWithChecksum ──

        [Fact]
        public void ParseWithChecksum_FrameWithoutChecksum_ReturnsSentinel()
        {
            var parser = new AdcDataParserService();
            // ValidFrameNoChecksum has no trailing 2-hex-digit checksum
            var result = parser.ParseWithChecksum(ValidFrameNoChecksum);

            Assert.Equal(-1.0, result.Force);
        }
    }
}
