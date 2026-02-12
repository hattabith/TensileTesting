using System.Globalization;
using System.Text.RegularExpressions;

namespace TensileTestingApp.Models
{
    public class ADCDataParser
    {
        private const string _patternCheckSum =
            "^(\\d{2}\\/\\d{2}\\/\\d{4} \\d{2}:\\d{2}:\\d{2}) >([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})(\\d{2})$";
        private const string _patternNoCheckSum =
            "^(\\d{2}\\/\\d{2}\\/\\d{4} \\d{2}:\\d{2}:\\d{2}) >([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})$";

        public TensileTestData ParseWithOutCheckSum(string data)
        {

            var matches = Regex.Match(data, _patternNoCheckSum);
            if (matches.Success)
                return new TensileTestData
                {
                    Timestamp = DateTime.Parse(matches.Groups[1].Value, CultureInfo.InvariantCulture),
                    Force = double.Parse(matches.Groups[2].Value.StartsWith("+") ? matches.Groups[2].Value[1..] : matches.Groups[2].Value, CultureInfo.InvariantCulture),
                    Length = double.Parse(matches.Groups[3].Value.StartsWith("+") ? matches.Groups[3].Value[1..] : matches.Groups[3].Value, CultureInfo.InvariantCulture)
                };
            return null;
        }
        public TensileTestData ParseWithCheckSum(string data)
        {
            var matches = Regex.Match(data, _patternCheckSum);
            if (matches.Success)
            {
                return new TensileTestData
                {
                    Timestamp = DateTime.Parse(matches.Groups[1].Value, CultureInfo.InvariantCulture),
                    Force = double.Parse(matches.Groups[2].Value.StartsWith("+") ? matches.Groups[2].Value[1..] : matches.Groups[2].Value, CultureInfo.InvariantCulture),
                    Length = double.Parse(matches.Groups[3].Value.StartsWith("+") ? matches.Groups[3].Value[1..] : matches.Groups[3].Value, CultureInfo.InvariantCulture)
                };
            }
            return null;
        }
    }
}
