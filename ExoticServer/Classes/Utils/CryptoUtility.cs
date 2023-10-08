using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class CryptoUtility
{
    private static readonly string key = "YourSecretKeyHere1234fgcxzasdfwa"; // 32 bytes for AES-256 - Will change from hard coded value eventually
    private static readonly string iv = "1234145632167844"; // 16 bytes for AES - Will change from hard coded value eventually

    public static byte[] Encrypt(byte[] data)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(data, 0, data.Length);
                }

                return msEncrypt.ToArray();
            }
        }
    }

    public static byte[] Decrypt(byte[] encryptedData)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    byte[] decryptedData = new byte[encryptedData.Length];
                    int bytesRead = csDecrypt.Read(decryptedData, 0, decryptedData.Length);

                    Array.Resize(ref decryptedData, bytesRead);
                    return decryptedData;
                }
            }
        }
    }

    // RSA encryption using the client's public key
    public static byte[] EncryptWithPublicKey(byte[] data, string publicKey)
    {
        using (RSA rsa = RSA.Create())
        {
            rsa.FromXmlString(publicKey);
            return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
        }
    }

    // RSA decryption using the server's private key
    public static byte[] DecryptWithPrivateKey(byte[] encryptedData, string privateKey)
    {
        using (RSA rsa = RSA.Create())
        {
            rsa.FromXmlString(privateKey);
            return rsa.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);
        }
    }

    public static string AesKey => key;
    public static string AesIV => iv;
}