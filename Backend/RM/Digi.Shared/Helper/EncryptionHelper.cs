using System;
using System.Security.Cryptography;
using System.Text;

namespace Digi.Shared.Helper
{
    public static class EncryptionHelper
    {
        private static readonly string DefaultKey = "0123456789ABCDEF0123456789ABCDEF";
        private static readonly string DefaultIv = "ABCDEF1234567890";

        /// <summary>Tenant SQL registry password — same Key/IV as Email Service / Company.Access <c>CompanyRegistry:TenantSqlEncryption</c>.</summary>
        public static string EncryptText(string plainText, string aesKeyUtf8, string aesIvUtf8)
        {
            var keyBytes = ToAesKeyBytes(aesKeyUtf8, 32);
            var ivBytes = ToAesKeyBytes(aesIvUtf8, 16);
            return EncryptCore(plainText, keyBytes, ivBytes);
        }

        /// <summary>Decrypt ciphertext produced by <see cref="EncryptText(string, string, string)"/>.</summary>
        public static string DecryptText(string encryptedText, string aesKeyUtf8, string aesIvUtf8)
        {
            var keyBytes = ToAesKeyBytes(aesKeyUtf8, 32);
            var ivBytes = ToAesKeyBytes(aesIvUtf8, 16);
            return DecryptCore(encryptedText, keyBytes, ivBytes);
        }

        public static string EncryptText(string plainText)
        {
            return EncryptCore(plainText, Encoding.UTF8.GetBytes(DefaultKey), Encoding.UTF8.GetBytes(DefaultIv));
        }

        public static string DecryptText(string encryptedText)
        {
            return DecryptCore(encryptedText, Encoding.UTF8.GetBytes(DefaultKey), Encoding.UTF8.GetBytes(DefaultIv));
        }

        private static string EncryptCore(string plainText, byte[] keyBytes, byte[] ivBytes)
        {
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = ivBytes;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using var encryptor = aes.CreateEncryptor();
            var inputBytes = Encoding.UTF8.GetBytes(plainText);
            var encrypted = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Convert.ToBase64String(encrypted);
        }

        private static string DecryptCore(string? encryptedText, byte[] keyBytes, byte[] ivBytes)
        {
            try
            {
                encryptedText = encryptedText?.Trim().Replace("\r", "").Replace("\n", "");

                if (string.IsNullOrWhiteSpace(encryptedText) || encryptedText.Length % 4 != 0 || !IsBase64StringInternal(encryptedText))
                    return encryptedText ?? "";

                var cipherBytes = Convert.FromBase64String(encryptedText);

                if (cipherBytes.Length % 16 != 0)
                    return encryptedText;

                using var aes = Aes.Create();
                aes.Key = keyBytes;
                aes.IV = ivBytes;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                var decrypted = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return encryptedText ?? "";
            }
        }

        private static byte[] ToAesKeyBytes(string utf8Secret, int requiredUtf8ByteLength)
        {
            var bytes = Encoding.UTF8.GetBytes(utf8Secret ?? "");
            if (bytes.Length != requiredUtf8ByteLength)
                throw new ArgumentException($"AES secret must be UTF-8 length exactly {requiredUtf8ByteLength} bytes (got {bytes.Length}).");
            return bytes;
        }

        private static bool IsBase64StringInternal(string str)
        {
            Span<byte> buffer = new Span<byte>(new byte[str.Length]);
            return Convert.TryFromBase64String(str, buffer, out _);
        }
    }
}
