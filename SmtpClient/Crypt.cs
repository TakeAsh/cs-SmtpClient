using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace TakeAsh {

    /// <summary>
    /// Encrypt, Decrypt utility
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>[c# - How to securely save username/password (local) - Stack Overflow](http://stackoverflow.com/questions/12657792)</item>
    /// <item>[Jon Galloway - Encrypting Passwords in a .NET app.config File](http://weblogs.asp.net/jongalloway//encrypting-passwords-in-a-net-app-config-file)</item>
    /// <item>[ProtectedData Class (System.Security.Cryptography)](https://msdn.microsoft.com/en-us/library/system.security.cryptography.protecteddata.aspx)</item>
    /// <item>[SecureString Class (System.Security)](https://msdn.microsoft.com/en-us/library/system.security.securestring.aspx)</item>
    /// </list>
    /// </remarks>
    public static class Crypt {

        public static byte[] Entropy { private get; set; }

        public static string Encrypt(SecureString plainText) {
            return Encrypt(ToInsecureString(plainText));
        }

        public static string Encrypt(string plainText) {
            return Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(plainText),
                Entropy,
                DataProtectionScope.CurrentUser
            ));
        }

        public static SecureString DecryptToSecureString(string encryptedText) {
            return ToSecureString(DecryptToString(encryptedText));
        }

        public static string DecryptToString(string encryptedText) {
            try {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                    Convert.FromBase64String(encryptedText),
                    Entropy,
                    DataProtectionScope.CurrentUser)
                );
            }
            catch {
                return null;
            }
        }

        public static SecureString ToSecureString(string input) {
            var secure = new SecureString();
            if (String.IsNullOrEmpty(input)) {
                secure.MakeReadOnly();
                return secure;
            }
            input.ToList()
                .ForEach(c => secure.AppendChar(c));
            secure.MakeReadOnly();
            return secure;
        }

        public static string ToInsecureString(SecureString input) {
            if (input == null) {
                return null;
            }
            var returnValue = string.Empty;
            var ptr = Marshal.SecureStringToBSTR(input);
            try {
                returnValue = Marshal.PtrToStringBSTR(ptr);
            }
            finally {
                Marshal.ZeroFreeBSTR(ptr);
            }
            return returnValue;
        }
    }
}
