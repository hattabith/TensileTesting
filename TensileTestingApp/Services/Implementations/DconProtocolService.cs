using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations
{
    public class DconProtocolService : IDconProtocolService
    {
        private string _address = "00";

        public void SetAddress(int address)
        {
            _address = address.ToString("D2");
        }

        public string GetReadCommand()
        {
            string command = "#" + _address;
            return command + CalcChecksum(command);
        }

        private static string CalcChecksum(string command)
        {
            int sum = 0;

            foreach (char c in command)
            {
                sum += (byte)c;
            }

            int checksum = sum & 0xFF;
            return checksum.ToString("X2");
        }
    }
}
