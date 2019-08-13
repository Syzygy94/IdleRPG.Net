using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IdleRPG.NET {
    public static class CryptoUtils {
        public static string GetDecryptedText(string initializationVector, string encryptionKey, string encryptedText) {
            return Decrypt(encryptedText, encryptionKey, initializationVector);
        }

        public static string GetEncryptedText(string initializationVector, string encryptionKey, string plainText) {
            return Encrypt(plainText, encryptionKey, initializationVector);
        }

        public static string GenerateVector() {
            RijndaelManaged cryptor = new RijndaelManaged();

            cryptor.GenerateIV();

            return Convert.ToBase64String(cryptor.IV);
        }

        public static string GenerateKey() {
            RijndaelManaged cryptor = new RijndaelManaged();

            cryptor.GenerateKey();

            return Convert.ToBase64String(cryptor.Key);
        }

        private static string Decrypt(string encryptedText, string key, string IV) {
            RijndaelManaged decryptor = new RijndaelManaged();
            UnicodeEncoding uEncode = new UnicodeEncoding();

            byte[] encryptedTextBytes = Convert.FromBase64String(encryptedText);
            MemoryStream plainTextStream = new MemoryStream();
            MemoryStream encryptedTextStream = new MemoryStream(encryptedTextBytes);

            decryptor.Key = Convert.FromBase64String(key);
            decryptor.IV = Convert.FromBase64String(IV);

            CryptoStream decryptedStream = new CryptoStream(encryptedTextStream, decryptor.CreateDecryptor(), CryptoStreamMode.Read);
            StreamWriter streamWriter = new StreamWriter(plainTextStream);
            StreamReader streamReader = new StreamReader(decryptedStream);
            streamWriter.Write(streamReader.ReadToEnd());
            streamWriter.Flush();
            decryptedStream.Clear();
            decryptor.Clear();

            return uEncode.GetString(plainTextStream.ToArray());
        }

        private static string Encrypt(string plainText, string key, string IV) {
            RijndaelManaged encryptor = new RijndaelManaged();
            UnicodeEncoding uEncode = new UnicodeEncoding();

            byte[] plainTextBytes = uEncode.GetBytes(plainText);
            MemoryStream encryptedTextStream = new MemoryStream();

            encryptor.Key = Convert.FromBase64String(key);
            encryptor.IV = Convert.FromBase64String(IV);

            CryptoStream encryptedStream = new CryptoStream(encryptedTextStream, encryptor.CreateEncryptor(), CryptoStreamMode.Write);
            encryptedStream.Write(plainTextBytes, 0, plainTextBytes.Length);
            encryptedStream.FlushFinalBlock();

            return Convert.ToBase64String(encryptedTextStream.ToArray());
        }
    }
}
