using System.Globalization;
using System.Text.RegularExpressions;
using TensileTestingApp.Models;
using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations
{
    public class AdcDataParserService : IDataParser
    {
        private const string PatternCheckSum =
            "^(\\d{4}\\-\\d{2}\\-\\d{2}.\\d{2}:\\d{2}:\\d{2}.\\d{7}\\+\\d{2}:\\d{2}) >([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([\\d[A-F]{2})";

        private const string PatternNoCheckSum =
            "^(\\d{4}\\-\\d{2}\\-\\d{2}.\\d{2}:\\d{2}:\\d{2}.\\d{7}\\+\\d{2}:\\d{2}) >([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})";

        public TensileTestData ParseWithoutChecksum(string data)
        {
            var matches = Regex.Match(data, PatternNoCheckSum);
            if (matches.Success)
            {
                return new TensileTestData
                {
                    Timestamp = DateTime.Parse(matches.Groups[1].Value, CultureInfo.InvariantCulture),
                    Force = 100d * -1d * double.Parse(matches.Groups[2].Value.StartsWith("+") ? matches.Groups[2].Value[1..] : matches.Groups[2].Value, CultureInfo.InvariantCulture),
                    Length = 10d * double.Parse(matches.Groups[3].Value.StartsWith("+") ? matches.Groups[3].Value[1..] : matches.Groups[3].Value, CultureInfo.InvariantCulture)
                };
            }

            return new TensileTestData
            {
                Timestamp = DateTime.Now,
                Force = -1d,
                Length = -1d
            };
        }

        public TensileTestData ParseWithChecksum(string data)
        {
            var matches = Regex.Match(data, PatternCheckSum);
            if (matches.Success)
            {
                return new TensileTestData
                {
                    Timestamp = DateTime.Parse(matches.Groups[1].Value, CultureInfo.InvariantCulture),
                    Force = 100d * -1d * double.Parse(matches.Groups[2].Value.StartsWith("+") ? matches.Groups[2].Value[1..] : matches.Groups[2].Value, CultureInfo.InvariantCulture),
                    Length = 10d * double.Parse(matches.Groups[3].Value.StartsWith("+") ? matches.Groups[3].Value[1..] : matches.Groups[3].Value, CultureInfo.InvariantCulture)
                };
            }

            return new TensileTestData
            {
                Timestamp = DateTime.Now,
                Force = -1d,
                Length = -1d
            };
        }
    }
}
