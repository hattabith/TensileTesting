using System.Globalization;
using System.Windows.Controls;

namespace TensileTestingApp
{
    public class IntRangeValidator : ValidationRule
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int intValue = 0;
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return new ValidationResult(false, "Value is required.");

            if (!int.TryParse(value.ToString(), out intValue))
                return new ValidationResult(false, "Invalid number.");

            if (intValue < Min || intValue > Max)
                return new ValidationResult(false, $"Value must be between {Min} and {Max}.");

            return ValidationResult.ValidResult;
        }
    }
}