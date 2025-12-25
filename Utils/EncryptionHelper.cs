using System;
using System.Security.Cryptography;
using System.Text;

namespace RDPManager.Utils
{
    /// <summary>
    /// 加密解密工具类
    /// 使用 AES 加密保护密码
    /// </summary>
    public static class EncryptionHelper
    {
        // 密钥（实际使用时建议使用机器特征码生成）
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("RDPManager2024Key123456789012345"); // 32 字节
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("1234567890123456"); // 16 字节

        /// <summary>
        /// 加密字符串
        /// </summary>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                    return Convert.ToBase64String(encryptedBytes);
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 解密字符串
        /// </summary>
        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
