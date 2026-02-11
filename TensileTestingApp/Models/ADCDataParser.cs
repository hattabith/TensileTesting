using System.Globalization;
using System.Text.RegularExpressions;

namespace TensileTestingApp.Models
{
    public class ADCDataParser
    {
        private const string _pattern = @"^(\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}) >([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})([+-]?\d{2}\.\d{3})(\d{2})$";
        public TensileTestData ParseWithOutCheckSum(string data)
        {

            var matches = Regex.Match(data, _pattern.Substring(0, 77));
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
            return new TensileTestData
            {
                Timestamp = DateTime.Parse(data.Substring(0, 19)),
                Force = double.Parse(data.Substring(20, 10)),
                Length = double.Parse(data.Substring(30, 10))
            };
        }
    }
}
