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
                    Timestamp = DateTime.Parse(matches.Groups[1].Value),
                    Force = double.Parse(matches.Groups[2].Value),
                    Length = double.Parse(matches.Groups[2].Value)
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
