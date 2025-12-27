using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Shared;
public static class SecurityExtension
{
    public static Dictionary<string, string[]> MapErrorMessage(this ModelStateDictionary ModelState)
    {
        return ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(kvp => kvp.Key.Replace("$.", ""), kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray());
    }

    public static string Sha256(this string input)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);

        return Convert.ToBase64String(hash);
    }

    public static string Sha256Hex(this string input)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);

        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }

    public static string Sha1(this string input)
    {
        return Encryption.SHA1Hash(input);
    }

    public static string UniqueId(this Guid guid, int length = 20)
    {
        StringBuilder builder = new();
        Enumerable
            .Range(65, 26)
            .Select(e => ((char)e).ToString())
            .Concat(Enumerable.Range(97, 26).Select(e => ((char)e).ToString()))
            .Concat(Enumerable.Range(0, 10).Select(e => e.ToString()))
            .OrderBy(e => Guid.NewGuid())
            .Take(length)
            .ToList()
            .ForEach(e => builder.Append(e));
        return builder.ToString();
    }
}
