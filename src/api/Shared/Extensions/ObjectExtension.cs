using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shared;

public static class ObjectExtension
{
    public static bool IsBoolean(this object property)
    {
        try
        {
            Convert.ToBoolean(property);
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsDecimal(this object property)
    {
        try
        {
            Convert.ToDecimal(property);
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsDateTime(this object property, string format = "MM/dd/yyyy")
    {
        try
        {
            DateTime.ParseExact(property.ToString(), format, CultureInfo.InvariantCulture);
            //Convert.ToDateTime(property);
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsNullableDateTime(this object property, string format = "MM/dd/yyyy")
    {
        if (property == null)
            return true;

        try
        {
            DateTime.ParseExact(property.ToString(), format, CultureInfo.InvariantCulture);
            //Convert.ToDateTime(property);
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsDouble(this object property)
    {
        try
        {
            Convert.ToDouble(property);
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsInt(this object property)
    {
        try
        {
            Convert.ToInt32(property);
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsInt16(this object property)
    {
        try
        {
            Convert.ToInt16(property);
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsLong(this object property)
    {
        try
        {
            Convert.ToInt64(property?.ToString());
            return true;
        }
        catch { }
        return false;
    }

    public static bool IsNullableLong(this object property)
    {
        if (property == null) return true;

        try
        {
            Convert.ToInt64(property.ToString());
            return true;
        }
        catch { }
        return false;
    }

    public static int ToInt32(this object property)
    {
        try
        {
            return Convert.ToInt32(property);
        }
        catch
        {
            return 0;
        }
    }

    public static byte ToByte(this object property)
    {
        try
        {
            return Convert.ToByte(property);
        }
        catch
        {
            return 0;
        }
    }

    public static int? ToNullableInt32(this object property)
    {
        try
        {
            return Convert.ToInt32(property);
        }
        catch
        {
            return null;
        }
    }

    public static decimal? ToNullableDecimal(this object property)
    {
        try
        {
            return Convert.ToDecimal(property);
        }
        catch
        {
            return null;
        }
    }

    public static decimal ToDecimal(this object property)
    {
        try
        {
            return Convert.ToDecimal(property);
        }
        catch
        {
            return 0;
        }
    }

    public static double? ToNullableDouble(this object property)
    {
        try
        {
            return Convert.ToDouble(property);
        }
        catch
        {
            return null;
        }
    }

    public static double ToDouble(this object property)
    {
        try
        {
            return Convert.ToDouble(property);
        }
        catch
        {
            return 0;
        }
    }

    public static long? ToNullableLong(this object property)
    {
        try
        {
            return Convert.ToInt64(property);
        }
        catch
        {
            return null;
        }
    }

    public static long ToLong(this object property)
    {
        try
        {
            return Convert.ToInt64(property);
        }
        catch
        {
            return 0;
        }
    }

    public static DateTime? ToNullableDateTime(this object property, string format = "MM/dd/yyyy")
    {
        try
        {
            return DateTime.ParseExact(property.ToString(), format, CultureInfo.InvariantCulture);
            //return Convert.ToDateTime(property);
        }
        catch
        {
            return null;
        }
    }

    public static string ToNullableTimeString(this object property, string format = "HH:mm")
    {
        if (property == null) return null;

        try
        {
            return DateTime.Parse(property.ToString()).ToString(format);
        }
        catch
        {
            return null;
        }
    }

    public static DateTime ToDateTime(this object property, string format = "MM/dd/yyyy")
    {
        if (property == null)
            return default;

        try
        {
            return DateTime.ParseExact(property.ToString(), format, CultureInfo.InvariantCulture);
            //return Convert.ToDateTime(property);
        }
        catch
        {
            return new DateTime();
        }
    }

    public static DateOnly ToDateOnly(this DateTime date)
    {
        return new DateOnly(date.Year, date.Month, date.Day);
    }

    public static bool ToBoolean(this object property)
    {
        try
        {
            return Convert.ToBoolean(property);
        }
        catch
        {
            return false;
        }
    }

    public static bool? ToNullableBoolean(this object property)
    {
        try
        {
            return Convert.ToBoolean(property);
        }
        catch
        {
            return null;
        }
    }

    public static string ToRoman(this object property)
    {
        var number = property.ToInt32();
        if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value between 1 and 3999");
        if (number < 1) return string.Empty;
        if (number >= 1000) return "M" + ToRoman(number - 1000);
        if (number >= 900) return "CM" + ToRoman(number - 900);
        if (number >= 500) return "D" + ToRoman(number - 500);
        if (number >= 400) return "CD" + ToRoman(number - 400);
        if (number >= 100) return "C" + ToRoman(number - 100);
        if (number >= 90) return "XC" + ToRoman(number - 90);
        if (number >= 50) return "L" + ToRoman(number - 50);
        if (number >= 40) return "XL" + ToRoman(number - 40);
        if (number >= 10) return "X" + ToRoman(number - 10);
        if (number >= 9) return "IX" + ToRoman(number - 9);
        if (number >= 5) return "V" + ToRoman(number - 5);
        if (number >= 4) return "IV" + ToRoman(number - 4);
        if (number >= 1) return "I" + ToRoman(number - 1);
        throw new ArgumentOutOfRangeException("something bad happened");
    }

    public static string ToJSONString(this object property)
    {
        return JsonConvert.SerializeObject(property ?? new { });
    }

    public static IDictionary<string, object> ToDictionary(this object obj)
    {
        var data = new Dictionary<string, object>();
        Type myType = obj.GetType();
        List<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
        props.ForEach(r => data.Add(r.Name, r.GetValue(obj, null)));
        return data;
    }

    public static T CloneJson<T>(this T source)
    {
        if (ReferenceEquals(source, null)) return default;

        var deserializeSettings = new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace };

        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source), deserializeSettings);
    }

    public static long ToUnixTimeMilliseconds(this DateTime date)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        TimeSpan diff = date - origin;
        return (long)diff.TotalMilliseconds;
    }

    public static string ReplaceAt(this string str, int index, int length, string replace)
    {
        return str.Remove(index, Math.Min(length, str.Length - index)).Insert(index, replace);
    }

    public static DateTime AddTime(this DateTime date, string time)
    {
        TimeSpan.TryParse(time, out TimeSpan result);
        return date.Add(result);
    }

    public static string ReplacePayrollCode(this string formula, string code, double value)
    {
        return formula.ReplacePayrollCode(code, value.ToString());
    }

    public static string ReplacePayrollCode(this string formula, string code, string value)
    {
        string pattern = @$"\b{code}";
        return Regex.Replace(formula, pattern, value);
    }

    public static double Round(this double value)
    {
        return Math.Round(value, MidpointRounding.ToPositiveInfinity);
    }

    public static double Round(this double value, double midPoint)
    {
        if (midPoint > 1)
        {
            var c = value % midPoint;
            return value - (value % midPoint);
        }

        return Math.Round(value - (midPoint - (midPoint * .01)), Math.Floor(Math.Log10(midPoint) + 1).ToInt32(), MidpointRounding.ToPositiveInfinity);
    }
}
