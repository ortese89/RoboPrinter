using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FrontEnds.RoboPrinter.Validations;

public class RequiredIntegerAttribute : ValidationAttribute
{
    public string ErrorMessageResourceName { get; set; }
    public Type ErrorMessageResourceType { get; set; }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value is int intValue && intValue == 0)
        {
            if (ErrorMessageResourceType != null && !string.IsNullOrEmpty(ErrorMessageResourceName))
            {
                var property = ErrorMessageResourceType.GetProperty(ErrorMessageResourceName, BindingFlags.Static | BindingFlags.Public);
                if (property != null)
                {
                    ErrorMessage = (string)property.GetValue(null, null);
                }
            }
            return new ValidationResult(ErrorMessage);
        }

        return ValidationResult.Success;
    }
}