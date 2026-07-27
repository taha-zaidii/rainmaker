using Org.BouncyCastle.Security;
using System.Text;

namespace Digi.Shared.Helper
{
    public static class HelperFun
    {
        public static string EncodeText(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            string result = System.Convert.ToBase64String(plainTextBytes);
            return result;
        }
        public static string EncryptId(int? encryptedNumber)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(encryptedNumber.ToString());
            return Convert.ToBase64String(plainTextBytes); 
                                                           
        }
        public static string EncryptText(string encryptedValue)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(encryptedValue.ToString());
            return Convert.ToBase64String(plainTextBytes); 
        }
        
        public static int Decrypt(string? encryptedValue)
        {
            var base64EncodedBytes = Convert.FromBase64String(encryptedValue);
            return int.Parse(Encoding.UTF8.GetString(base64EncodedBytes));
        }
    }

}
