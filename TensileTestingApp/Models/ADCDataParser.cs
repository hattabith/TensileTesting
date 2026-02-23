using System.Globalization;
using System.Text.RegularExpressions;

namespace TensileTestingApp.Models
{
    public class ADCDataParser
    {
        // New RegExp
        // ^(\d{4}\-\d{2}\-\d{2}.\d{2}:\d{2}:\d{2}.\d{7}\+\d{2}:\d{2}) >([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([\d[A-F]{2})$
        // 2026-02-20T16:11:09.9247019+02:00 >+00.001+00.000-00.000-00.000-00.000-00.000-00.000-00.00093

        // Old RegExp
        // ^(\\d{2}\\/\\d{2}\\/\\d{4} \\d{2}:\\d{2}:\\d{2}) >([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})(\\d[A-F]{2})
        private const string _patternCheckSum =
            "^(\\d{4}\\-\\d{2}\\-\\d{2}.\\d{2}:\\d{2}:\\d{2}.\\d{7}\\+\\d{2}:\\d{2}) >([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([\\d[A-F]{2})";
        private const string _patternNoCheckSum =
            "^(\\d{4}\\-\\d{2}\\-\\d{2}.\\d{2}:\\d{2}:\\d{2}.\\d{7}\\+\\d{2}:\\d{2}) >([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})([+-]?\\d{2}\\.\\d{3})";

        public TensileTestData ParseWithOutCheckSum(string data)
        {

            var matches = Regex.Match(data, _patternNoCheckSum);
            if (matches.Success)
                return new TensileTestData
                {
                    Timestamp = DateTime.Parse(matches.Groups[1].Value, CultureInfo.InvariantCulture),
                    Force = 100d * double.Parse(matches.Groups[2].Value.StartsWith("+") ? matches.Groups[2].Value[1..] : matches.Groups[2].Value, CultureInfo.InvariantCulture),
                    Length = 10d * double.Parse(matches.Groups[3].Value.StartsWith("+") ? matches.Groups[3].Value[1..] : matches.Groups[3].Value, CultureInfo.InvariantCulture)
                };
            return new TensileTestData
            {
                Timestamp = DateTime.Now,
                Force = -1d,
                Length = -1d
            };
        }
        public TensileTestData ParseWithCheckSum(string data)
        {
            var matches = Regex.Match(data, _patternCheckSum);
            if (matches.Success)
            {
                return new TensileTestData
                {
                    Timestamp = DateTime.Parse(matches.Groups[1].Value, CultureInfo.InvariantCulture),
                    Force = 100d * double.Parse(matches.Groups[2].Value.StartsWith("+") ? matches.Groups[2].Value[1..] : matches.Groups[2].Value, CultureInfo.InvariantCulture),
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
