namespace TensileTestingApp.Models;
    public class DCONProtocol
    {
        string _address;
        public DCONProtocol(int address)
        {
            _address = address.ToString("D2");  // Add number of zero before integer to make it 2 digits, for example 1 becomes 01, 2 becomes 02, etc.
            // TODO: Implement DCON protocol here


            // TODO: Implement DCON command list and functionality
            // TODO: Add command format to documentation
            // TODO: Thinking aabout XML documentation and use DocFX to generate documentation

            /* Command format
             *  -----------------------------------------------------------
             * | Leading Character | Module Address | Data | [CHKSUM] | CR |
             * ------------------------------------------------------------
             * 
             * IMPORTANT: All characters should be in upper case. All commands must end with a Carriage Return (CR) character (ASCII 13)
             * 
             * Most important commands:
             * #AA - Read the analog input for all channels
             * -- Valid response: >(Data)[CHKSUM](CR)
             * -- Example: >+025.12+020.45+012.78+018.97+003.24+015.35+008.07+014.79
             * #AAN - Read the analog input for channel N (N = 0 - 7)
             * -- Valid response: >(Data)[CHKSUM](CR)
             * -- Example: >+025.12
             * $AAA - Read the analog input for all channels in HEX format
             * -- Valid response: >(Data)[CHKSUM](CR)
             * -- Example:  >0000012301257FFF1802744F98238124
             * $AAF - Read the firmware version
             * -- Valid response: !AA(Data)[CHKSUM](CR)
             * -- Example:  !01A2.0
             * $AAM - Read the module name
             * -- Valid response: !AA(Name)[CHKSUM](CR)
             * -- Example:  !017017
             * $AAP - Read the protocol
             * -- Valid response: !AASC[CHKSUM](CR)
             * ---- AA - Module address
             * ---- S - Protocol type (0 = Only DCON, 1 = DCON and Modbus RTU)
             * ---- C - Current protocol saved in EPROM (0 = DCON, 1 = Modbus RTU)
             * -- Example:  !0110
             * @AAS - Read the differential/single-ended connection mode status
             * -- Valid response: !AAN(MODE)[CHKSUM](CR)
             * ---- N - Current connection mode (0 = Differential, 1 = Single-ended)
             * -- Example:  !010
             * ~** - Informs all modules that the host is OK
             * 
             * 
             * # - ChannelsReadDelimiter
             * $ - SystemQueryDelimiter
             * @ - ConfigStatusDelimiter
             * ~ - BroadCastDelimiter
             * 
             * 
             */

        }

        public string GetReadCommand()
        {
            string command = "#" + _address;
            return command + CalcChecksum(command);
        }

        private string CalcChecksum(string command)
        {
            int sum = 0;

            foreach (char c in command)
            {
                sum += (byte)c;
            }
            int checksum = sum & 0xFF;

            return checksum.ToString("X2");  // Convert to hexadecimal string with 2 digits
        }
    }
