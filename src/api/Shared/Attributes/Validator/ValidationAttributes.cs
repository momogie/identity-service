using System.Collections;
using System.Text.RegularExpressions;

namespace Shared;

public class PropertyOptions : ValidationAttribute
{
    public string[] Options { get; set; }
    public PropertyOptions(params string[] options)
    {
        Options = options;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        if(value.ToString().StartsWith("{") && value.ToString().EndsWith("}"))
            return ValidationResult.Success;

        if (!Options.Select(p => p.ToUpper()).Contains(value.ToString().ToUpper()))
            return new ValidationResult($"The {validationContext.DisplayName} is invalid. Options: {string.Join(", ", Options)}", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}

public class ListRequired : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return new ValidationResult($"The {validationContext.DisplayName} is required.", new[] { validationContext.MemberName });

        if (!value.GetType().IsGenericType)
            return new ValidationResult($"The {validationContext.DisplayName} is invalid.", new[] { validationContext.MemberName });

        //if(((IList<dynamic>)value).Count == 0)
        //    return new ValidationResult($"The {validationContext.DisplayName} is required.");

        return ValidationResult.Success;
    }
}

public class NumberGreaterThan : ValidationAttribute
{
    protected double Compare { get; set; }
    public NumberGreaterThan(double compareValue)
    {
        Compare = compareValue;
    }
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        if (value.ToDouble() < Compare)
            return new ValidationResult($"The {validationContext.DisplayName} should greater than {Compare}.", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}

public class RequiredIfNull : ValidationAttribute
{
    protected string CompareProperty;
    public RequiredIfNull(string property)
    {
        CompareProperty = property;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(CompareProperty);
        if (property == null)
            throw new ArgumentException(@"Property " + CompareProperty + " not found");

        if (property.GetValue(validationContext.ObjectInstance) == null && value == null)
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}

public class RequiredIfNotNull : ValidationAttribute
{
    protected string[] CompareProperty;
    public RequiredIfNotNull(params string[] property)
    {
        CompareProperty = property;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        foreach (var r in CompareProperty)
        {
            var property = validationContext.ObjectType.GetProperty(r);
            if (property == null)
                throw new ArgumentException(@"Property " + r + " not found");

            if (property.GetValue(validationContext.ObjectInstance) != null && value == null)
                return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });
        }

        return ValidationResult.Success;
    }
}

public class RequiredIfNotNullOrZero : ValidationAttribute
{
    protected string[] CompareProperty;
    public RequiredIfNotNullOrZero(params string[] property)
    {
        CompareProperty = property;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        foreach (var r in CompareProperty)
        {
            var property = validationContext.ObjectType.GetProperty(r);
            if (property == null)
                throw new ArgumentException(@"Property " + r + " not found");

            var val = property.GetValue(validationContext.ObjectInstance);

            if (val != null && value == null && val.ToString().ToLong() != 0)
                return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });
        }

        return ValidationResult.Success;
    }
}

public class RequiredIfTrue : ValidationAttribute
{
    public string CompareProperty;
    public RequiredIfTrue(string property)
    {
        CompareProperty = property;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(CompareProperty);
        if (property == null)
            throw new ArgumentException(@"Property " + CompareProperty + " not found");

        bool isTrue = property.GetValue(validationContext.ObjectInstance).ToBoolean();

        if (isTrue && string.IsNullOrEmpty(value?.ToString()))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}

public class RequiredIfFalse : ValidationAttribute
{
    public string CompareProperty;

    public RequiredIfFalse(string property)
    {
        CompareProperty = property;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(CompareProperty);
        if (property == null)
            throw new ArgumentException(@"Property " + CompareProperty + " not found");

        bool isTrue = property.GetValue(validationContext.ObjectInstance).ToBoolean();

        if (!isTrue && (value == null || string.IsNullOrWhiteSpace(value?.ToString())))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}

public class RequiredIfEqual : ValidationAttribute
{
    public string CompareProperty;
    public object Value;
    public RequiredIfEqual(string property, object value)
    {
        CompareProperty = property;
        Value = value;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(CompareProperty);
        if (property == null)
            throw new ArgumentException(@"Property " + CompareProperty + " not found");

        var cmp = property.GetValue(validationContext.ObjectInstance);
        if (cmp == null)
            return ValidationResult.Success;

        bool isTrue = property.GetValue(validationContext.ObjectInstance).ToString() == Value.ToString();

        if ((!isTrue || value is Array) && (value is null ? false : (value as Array)?.Length == 0))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        if (isTrue && value is not null && (value.GetType().IsGenericType ? (value as IList).Count == 0 : false))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        if (isTrue && (value == null ? true : string.IsNullOrEmpty(value.ToString())))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}

public class RequiredIfNotEqual : ValidationAttribute
{
    public string CompareProperty;
    public object Value;
    public RequiredIfNotEqual(string property, object value)
    {
        CompareProperty = property;
        Value = value;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(CompareProperty);
        if (property == null)
            throw new ArgumentException(@"Property " + CompareProperty + " not found");

        var cmp = property.GetValue(validationContext.ObjectInstance);
        if (cmp == null)
            return ValidationResult.Success;

        bool isTrue = property.GetValue(validationContext.ObjectInstance).ToString() != Value.ToString();

        if ((!isTrue || value is Array) && (value is null ? false : (value as Array)?.Length == 0))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        if (isTrue && value is not null && (value.GetType().IsGenericType ? (value as IList).Count == 0 : false))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        if (isTrue && (value == null ? true : string.IsNullOrEmpty(value.ToString())))
            return new ValidationResult($"The {validationContext.DisplayName} field is required", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}

public class Code : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        //if(((IList<dynamic>)value).Count == 0)
        //    return new ValidationResult($"The {validationContext.DisplayName} is required.");

        return ValidationResult.Success;
    }
}

public class EmailFormat : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success;

        //string regex = @"^([\w-\.]+)|([+0-9]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
        //string regex = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";
        //string regex = @"^(?!\.)[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        //string regex = @"^(?!\.)[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}(?![0-9]*$)(?![a-zA-Z]*$)";
        string regex = @"^(?!\.)[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}(?![0-9]*$)(?![a-zA-Z]*\.[0-9]*$)";
        //string regex = @"^(?!\.)[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{3,}(?![0-9]*$)(?![a-zA-Z]*$)";
        if (!Regex.IsMatch(value?.ToString(), regex, RegexOptions.IgnoreCase))
            return new ValidationResult($"The {validationContext.DisplayName} field is invalid email address.", new[] { validationContext.MemberName });

        return ValidationResult.Success;
    }
}


public class PhoneFormat : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        try
        {
            Convert.ToInt64(value.ToString());
        }
        catch
        {
            return new ValidationResult($"The {validationContext.DisplayName} is invalid.", new List<string> { validationContext.MemberName });
        }

        return ValidationResult.Success;
    }
}

public class NumberFormat : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success;

        try
        {
            var c = Convert.ToInt64(value.ToString());
            return ValidationResult.Success;
        }
        catch (FormatException)
        {
            return new ValidationResult($"The {validationContext.DisplayName} field is invalid number.", new List<string> { validationContext.MemberName });
        }
    }
}