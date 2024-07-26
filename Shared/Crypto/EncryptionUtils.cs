using System.Security.Cryptography;

namespace Apachi.Shared.Crypto;

public static class EncryptionUtils
{
    public static async Task<byte[]> SymmetricEncryptAsync(byte[] value, byte[] aesKey)
    {
        if (aesKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(aesKey));
        }

        await using var inputStream = new MemoryStream(value);
        await using var outputStream = new MemoryStream();

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = RandomNumberGenerator.GetBytes(16);

        using var encryptor = aes.CreateEncryptor();
        await using var aesStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);

        await outputStream.WriteAsync(aes.IV);
        await inputStream.CopyToAsync(aesStream);
        await aesStream.FlushFinalBlockAsync();

        return outputStream.ToArray();
    }

    public static async Task<byte[]> SymmetricDecryptAsync(byte[] encrypted, byte[] aesKey)
    {
        if (aesKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(aesKey));
        }

        await using var inputStream = new MemoryStream(encrypted);
        await using var outputStream = new MemoryStream();

        var iv = new byte[16];
        await inputStream.ReadExactlyAsync(iv);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        await using var aesStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);

        await aesStream.CopyToAsync(outputStream);
        return outputStream.ToArray();
    }

    public static async Task<byte[]> SymmetricEncryptAndMacAsync(byte[] value, byte[] aesKey, byte[] hmacKey)
    {
        if (aesKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(aesKey));
        }

        if (hmacKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(hmacKey));
        }

        await using var inputStream = new MemoryStream(value);
        await using var outputStream = new MemoryStream();

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = RandomNumberGenerator.GetBytes(16);

        using var hmac = new HMACSHA256(hmacKey);
        using var encryptor = aes.CreateEncryptor();

        await using var hmacStream = new CryptoStream(outputStream, hmac, CryptoStreamMode.Write);
        await using var aesStream = new CryptoStream(hmacStream, encryptor, CryptoStreamMode.Write);

        // Advance the output stream with the hash size, because we want to write the HMAC at 0.
        outputStream.Position += hmac.HashSize / 8;

        await hmacStream.WriteAsync(aes.IV);
        await inputStream.CopyToAsync(aesStream);
        await aesStream.FlushFinalBlockAsync();

        outputStream.Position = 0;
        await outputStream.WriteAsync(hmac.Hash);

        return outputStream.ToArray();
    }

    public static async Task<byte[]> SymmetricDecryptAndVerifyAsync(byte[] encrypted, byte[] aesKey, byte[] hmacKey)
    {
        if (aesKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(aesKey));
        }

        if (hmacKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(hmacKey));
        }

        await using var inputStream = new MemoryStream(encrypted);
        await using var outputStream = new MemoryStream();

        var hash = new byte[HMACSHA256.HashSizeInBytes];
        await inputStream.ReadExactlyAsync(hash);

        using var hmac = new HMACSHA256(hmacKey);
        await using var hmacStream = new CryptoStream(inputStream, hmac, CryptoStreamMode.Read);

        var iv = new byte[16];
        await hmacStream.ReadExactlyAsync(iv);

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        await using var aesStream = new CryptoStream(hmacStream, decryptor, CryptoStreamMode.Read);

        await aesStream.CopyToAsync(outputStream);
        var hashesEqual = hmac.Hash!.SequenceEqual(hash);

        if (!hashesEqual)
        {
            throw new CryptographicException("The HMAC in the input buffer does the match the computed HMAC.");
        }

        return outputStream.ToArray();
    }

    public static async Task<byte[]> AsymmetricEncryptAsync(byte[] value, byte[] publicKey)
    {
        using var rsa = RSA.Create(Constants.DefaultRSAKeySize);
        rsa.ImportRSAPublicKey(publicKey, out _);
        var encrypted = await Task.Run(() => rsa.Encrypt(value, RSAEncryptionPadding.OaepSHA256));
        return encrypted;
    }

    public static async Task<byte[]> AsymmetricDecryptAsync(byte[] encrypted, byte[] privateKey)
    {
        using var rsa = RSA.Create(Constants.DefaultRSAKeySize);
        rsa.ImportRSAPrivateKey(privateKey, out _);
        var decrypted = await Task.Run(() => rsa.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256));
        return decrypted;
    }
}
