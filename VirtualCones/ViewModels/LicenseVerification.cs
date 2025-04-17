using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AOS_VirtualCones_MCB.ViewModels
{
    internal class LicenseVerification
    {

        /// <summary>
        /// Validates the license using the provided admin key and user string.
        /// </summary>
        /// <param name="adminKey">The admin encryption key.</param>
        /// <param name="userKey">The expected user key to match against.</param>
        /// <returns>True if the license is valid, otherwise false.</returns>
        public bool ValidateLicense(string adminKey, string userKey, string parentDirectory)
        {
            try
            {
                string LicenseFileName = Path.Combine(parentDirectory, "license.txt");
                // Read the license file
                if (!File.Exists(LicenseFileName))
                    throw new FileNotFoundException("License file not found.");

                string encryptedLicense = File.ReadAllText(LicenseFileName);

                // Decrypt the license
                string decryptedKey = Decrypt(encryptedLicense, adminKey);

                // Validate the user key
                return decryptedKey == userKey;
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"License verification failed: {ex.Message}");
                return false;
            }
        }

        private string Decrypt(string encryptedText, string adminKey)
        {
            byte[] keyBytes = GenerateKeyFromAdminKey(adminKey);
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = new byte[16]; // Must match IV used during encryption
                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(Convert.FromBase64String(encryptedText)))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private byte[] GenerateKeyFromAdminKey(string adminKey)
        {
            // Create a 32-byte encryption key from the admin key
            return Encoding.UTF8.GetBytes(adminKey.PadRight(32));
        }

    }
}
