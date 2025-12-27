using System.Text.RegularExpressions;

namespace Shared;

public class IsRequired : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return new ValidationResult($"The {validationContext.DisplayName} field is required.", [validationContext.MemberName]);

        if (string.IsNullOrWhiteSpace(value.ToString()))
            return new ValidationResult($"The {validationContext.DisplayName} field is required.", [validationContext.MemberName]);

        return ValidationResult.Success;
    }
}

public class IsMaxLength : ValidationAttribute
{

}

public class IsMinLength : ValidationAttribute
{

}

public class IsPropertyInfo : ValidationAttribute
{

}

public class IsMatch : ValidationAttribute
{

}

public class IsEqual : ValidationAttribute
{

}

public class CodeFormat : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        if (!Regex.IsMatch(value.ToString(), @"^\b[A-Za-z_0-9]+\b$"))
            return new ValidationResult($"The {validationContext.DisplayName} field is invalid.", [validationContext.MemberName]);

        return ValidationResult.Success;
    }
}

public class PayrollCodeFormat : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        if (!Regex.IsMatch(value.ToString(), @"^\b[A-Za-z_0-9]+\b$"))
            return new ValidationResult($"The {validationContext.DisplayName} field is invalid.", [validationContext.MemberName]);

        return ValidationResult.Success;
    }
}

