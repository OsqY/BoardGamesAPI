using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MyBGList.Attributes
{
    public class OnlyLettersValidatorAttribute : ValidationAttribute
    {
        public bool UseRegex { get; set; } = false;

        public OnlyLettersValidatorAttribute()
          : base("Value must contain only letters(no spaces, digits or other chars.") { }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var strValue = value as string;

            if (!string.IsNullOrEmpty(strValue) && (
                  UseRegex && Regex.IsMatch(strValue, "^[A-Za-z]+$"))
                || strValue.All(char.IsLetter))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage);
        }
    }
}
