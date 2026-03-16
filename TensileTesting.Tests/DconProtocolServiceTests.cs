using TensileTestingApp.Services.Implementations;

namespace TensileTesting.Tests
{
    // Checksum verification:
    //   command = "#" + address.ToString("D2")
    //   checksum = (sum of ASCII bytes) & 0xFF, formatted as 2 uppercase hex digits
    //
    //   address 0  → "#00" → 35+48+48 = 131 = 0x83 → "#0083"
    //   address 1  → "#01" → 35+48+49 = 132 = 0x84 → "#0184"
    //   address 16 → "#16" → 35+49+54 = 138 = 0x8A → "#168A"

    public class DconProtocolServiceTests
    {
        // ── GetReadCommand format ─────────────────────────────────────────────

        [Fact]
        public void GetReadCommand_DefaultAddress_StartsWithHashAndZeroZero()
        {
            var service = new DconProtocolService();
            Assert.StartsWith("#00", service.GetReadCommand());
        }

        [Fact]
        public void GetReadCommand_DefaultAddress_HasTwoHexDigitChecksum()
        {
            var service = new DconProtocolService();
            string cmd = service.GetReadCommand();
            // 5 chars: '#' + 2 address digits + 2 checksum digits
            Assert.Equal(5, cmd.Length);
            Assert.Matches("^#[0-9A-F]{4}$", cmd);
        }

        // ── Checksum correctness ──────────────────────────────────────────────

        [Theory]
        [InlineData(0, "#0083")]
        [InlineData(1, "#0184")]
        [InlineData(16, "#168A")]
        public void GetReadCommand_ReturnsCorrectChecksummedCommand(int address, string expected)
        {
            var service = new DconProtocolService();
            service.SetAddress(address);
            Assert.Equal(expected, service.GetReadCommand());
        }

        // ── SetAddress changes subsequent commands ────────────────────────────

        [Fact]
        public void SetAddress_ChangesAddress_AffectsSubsequentCommand()
        {
            var service = new DconProtocolService();
            service.SetAddress(0);
            string first = service.GetReadCommand();   // "#0083"

            service.SetAddress(1);
            string second = service.GetReadCommand();  // "#0184"

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void SetAddress_SameAddressTwice_CommandRemainsStable()
        {
            var service = new DconProtocolService();
            service.SetAddress(1);
            string first = service.GetReadCommand();
            string second = service.GetReadCommand();
            Assert.Equal(first, second);
        }
    }
}
