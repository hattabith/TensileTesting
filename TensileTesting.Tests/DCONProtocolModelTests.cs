using TensileTestingApp.Models;

namespace TensileTesting.Tests
{
    // Checksum verification (same algorithm as DCONProtocol.CalcChecksum):
    //   command = "#" + address.ToString("D2")
    //   checksum = (sum of ASCII bytes) & 0xFF, formatted as 2 uppercase hex digits
    //
    //   address 0  → "#00" → 35+48+48 = 131 = 0x83 → "#0083"
    //   address 1  → "#01" → 35+48+49 = 132 = 0x84 → "#0184"
    //   address 16 → "#16" → 35+49+54 = 138 = 0x8A → "#168A"

    public class DCONProtocolModelTests
    {
        // ── Checksum correctness ──────────────────────────────────────────────

        [Theory]
        [InlineData(0, "#0083")]
        [InlineData(1, "#0184")]
        [InlineData(16, "#168A")]
        public void GetReadCommand_ReturnsCorrectChecksummedCommand(int address, string expected)
        {
            var protocol = new DCONProtocol(address);
            Assert.Equal(expected, protocol.GetReadCommand());
        }

        // ── Format assertions ─────────────────────────────────────────────────

        [Fact]
        public void GetReadCommand_StartsWithHash()
        {
            var protocol = new DCONProtocol(1);
            Assert.StartsWith("#", protocol.GetReadCommand());
        }

        [Fact]
        public void GetReadCommand_HasTwoHexDigitChecksum()
        {
            var protocol = new DCONProtocol(1);
            string cmd = protocol.GetReadCommand();
            // 5 chars: '#' + 2 address digits + 2 checksum digits
            Assert.Equal(5, cmd.Length);
            Assert.Matches("^#[0-9A-F]{4}$", cmd);
        }

        [Fact]
        public void GetReadCommand_IsDeterministic()
        {
            var protocol = new DCONProtocol(5);
            Assert.Equal(protocol.GetReadCommand(), protocol.GetReadCommand());
        }
    }
}
