using System.Security.Cryptography;
using System.Text;

namespace Shared;

public class Encryption
{
    const string DefaultKey = "CGK^&gKh12&*FRx1==";
    const string DefaultSalt = "4ìXaC2»FÏ5xx§#";

    /// <summary>
    /// Method which does the encryption using Rijndeal algorithm
    /// </summary>
    /// <param name="InputText">Data to be encrypted</param>
    /// <param name="Password">The string to used for making the key.The same string
    /// should be used for making the decrpt key</param>
    /// <returns>Encrypted Data</returns>
    public static string Encrypt(string InputText, string Password = null)
    {
        if (Password == null)
            Password = DefaultKey;

        byte[] Salt = Encoding.ASCII.GetBytes(DefaultSalt);
        PasswordDeriveBytes SecretKey = new(Password, Salt);

        byte[] encrypted;

        // Create an AesManaged object
        // with the specified key and IV.
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = SecretKey.GetBytes(32);
            aesAlg.IV = SecretKey.GetBytes(16);

            // Create an encryptor to perform the stream transform.
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for encryption.
            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new(csEncrypt))
            {
                swEncrypt.Write(InputText);
            }
            encrypted = msEncrypt.ToArray();
        }

        // Return the encrypted bytes from the memory stream.
        return Convert.ToBase64String(encrypted);
    }

    public static string EncryptToHexString(string InputText, string Password = null)
    {
        if (Password == null)
            Password = DefaultKey;

        byte[] Salt = Encoding.ASCII.GetBytes(DefaultSalt);
        PasswordDeriveBytes SecretKey = new(Password, Salt);

        byte[] encrypted;

        // Create an AesManaged object
        // with the specified key and IV.
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = SecretKey.GetBytes(32);
            aesAlg.IV = SecretKey.GetBytes(16);

            // Create an encryptor to perform the stream transform.
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for encryption.
            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new(csEncrypt))
            {
                swEncrypt.Write(InputText);
            }
            encrypted = msEncrypt.ToArray();
        }

        // Return the encrypted bytes from the memory stream.
        return BitConverter.ToString(encrypted).Replace("-", string.Empty);
    }

    public static byte[] EncryptToBytes(string InputText, string Password = null)
    {
        if (Password == null)
            Password = DefaultKey;

        byte[] Salt = Encoding.ASCII.GetBytes(DefaultSalt);
        PasswordDeriveBytes SecretKey = new(Password, Salt);

        byte[] encrypted;

        // Create an AesManaged object
        // with the specified key and IV.
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = SecretKey.GetBytes(32);
            aesAlg.IV = SecretKey.GetBytes(16);

            // Create an encryptor to perform the stream transform.
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for encryption.
            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new(csEncrypt))
            {
                swEncrypt.Write(InputText);
            }
            encrypted = msEncrypt.ToArray();
        }

        // Return the encrypted bytes from the memory stream.
        return encrypted;
    }

    /// <summary>
    /// Method which does the encryption using Rijndeal algorithm.This is for decrypting the data
    /// which has orginally being encrypted using the above method
    /// </summary>
    /// <param name="InputText">The encrypted data which has to be decrypted</param>
    /// <param name="Password">The string which has been used for encrypting.The same string
    /// should be used for making the decrypt key</param>
    /// <returns>Decrypted Data</returns>
    public static string Decrypt(string InputText, string Password = null)
    {
        if (string.IsNullOrEmpty(InputText)) return InputText;
        if (Password == null)
            Password = DefaultKey;

        byte[] EncryptedData = Convert.FromBase64String(InputText);

        byte[] Salt = Encoding.ASCII.GetBytes(DefaultSalt);
        PasswordDeriveBytes SecretKey = new(Password, Salt);

        string plaintext = null;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = SecretKey.GetBytes(32);
            aesAlg.IV = SecretKey.GetBytes(16);

            // Create a decryptor to perform the stream transform.
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for decryption.
            using MemoryStream msDecrypt = new(EncryptedData);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);

            // Read the decrypted bytes from the decrypting stream
            // and place them in a string.
            plaintext = srDecrypt.ReadToEnd();
        }

        return plaintext;
    }

    public static string DecryptFromHexString(string InputText, string Password = null)
    {
        if (string.IsNullOrEmpty(InputText)) return InputText;
        if (Password == null)
            Password = DefaultKey;

        byte[] EncryptedData = Enumerable.Range(0, InputText.Length)
                 .Where(x => x % 2 == 0)
                 .Select(x => Convert.ToByte(InputText.Substring(x, 2), 16))
                 .ToArray();

        byte[] Salt = Encoding.ASCII.GetBytes(DefaultSalt);
        PasswordDeriveBytes SecretKey = new(Password, Salt);

        string plaintext = null;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = SecretKey.GetBytes(32);
            aesAlg.IV = SecretKey.GetBytes(16);

            // Create a decryptor to perform the stream transform.
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for decryption.
            using MemoryStream msDecrypt = new(EncryptedData);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);

            // Read the decrypted bytes from the decrypting stream
            // and place them in a string.
            plaintext = srDecrypt.ReadToEnd();
        }

        return plaintext;
    }

    public static string DecryptFromBytes(byte[] input, string Password = null)
    {
        if (input == null) return default;

        if (Password == null)
            Password = DefaultKey;

        byte[] EncryptedData = input;

        byte[] Salt = Encoding.ASCII.GetBytes(DefaultSalt);
        PasswordDeriveBytes SecretKey = new(Password, Salt);

        string plaintext = null;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = SecretKey.GetBytes(32);
            aesAlg.IV = SecretKey.GetBytes(16);

            // Create a decryptor to perform the stream transform.
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            // Create the streams used for decryption.
            using MemoryStream msDecrypt = new(EncryptedData);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);

            // Read the decrypted bytes from the decrypting stream
            // and place them in a string.
            plaintext = srDecrypt.ReadToEnd();
        }

        return plaintext;
    }

    public static string SHA256Hash(string InputText)
    {
        return BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(InputText))).Replace("-", "");
    }

    public static byte[] SHA256HashToBytes(string InputText)
    {
        return SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(InputText));
    }

    public static string SHA1Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return default;

        var hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
