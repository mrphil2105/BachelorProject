using System.Security.Cryptography;

namespace Apachi.Shared.Crypto;

public static class EncryptionUtils
{
    public static async Task<byte[]> SymmetricEncryptAsync(byte[] value, byte[] key)
    {
        await using var inputStream = new MemoryStream(value);
        return await SymmetricEncryptAsync(inputStream, key);
    }

    public static async Task<byte[]> SymmetricEncryptAsync(Stream inputStream, byte[] key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(key));
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = RandomNumberGenerator.GetBytes(16);

        using var transform = aes.CreateEncryptor();
        await using var outputStream = new MemoryStream();
        await using var cryptoStream = new CryptoStream(outputStream, transform, CryptoStreamMode.Write);

        await outputStream.WriteAsync(aes.IV).ConfigureAwait(false);
        await inputStream.CopyToAsync(cryptoStream).ConfigureAwait(false);
        await cryptoStream.FlushFinalBlockAsync();
        return outputStream.ToArray();
    }

    public static async Task<byte[]> SymmetricDecryptAsync(byte[] encrypted, byte[] key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(key));
        }

        using var inputStream = new MemoryStream(encrypted);
        var iv = new byte[16];
        await inputStream.ReadAsync(iv).ConfigureAwait(false);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var transform = aes.CreateDecryptor();
        await using var outputStream = new MemoryStream();
        await using var cryptoStream = new CryptoStream(inputStream, transform, CryptoStreamMode.Read);

        await cryptoStream.CopyToAsync(outputStream).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    public static byte[] AsymmetricEncrypt(byte[] data, byte[] publicKey)
    {
        using var rsa = RSA.Create(Constants.DefaultRSAKeySize);
        rsa.ImportRSAPublicKey(publicKey, out _);
        var encryptedBytes = rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        return encryptedBytes;
    }

    public static byte[] AsymmetricDecrypt(byte[] data, byte[] privateKey)
    {
        using var rsa = RSA.Create(Constants.DefaultRSAKeySize);
        rsa.ImportRSAPrivateKey(privateKey, out _);
        var decryptedBytes = rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
        return decryptedBytes;
    }
}
