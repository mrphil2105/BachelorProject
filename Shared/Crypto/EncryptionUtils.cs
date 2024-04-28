using System.Security.Cryptography;

namespace Apachi.Shared.Crypto;

public static class EncryptionUtils
{
    public static async Task<byte[]> SymmetricEncryptAsync(byte[] value, byte[] key, byte[]? hmacKey)
    {
        await using var inputStream = new MemoryStream(value);
        return await SymmetricEncryptAsync(inputStream, key, hmacKey);
    }

    public static async Task<byte[]> SymmetricEncryptAsync(Stream inputStream, byte[] aesKey, byte[]? hmacKey)
    {
        if (aesKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(aesKey));
        }

        if (hmacKey != null && hmacKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(hmacKey));
        }

        using var aes = Aes.Create();
        aes.Key = aesKey;
        aes.IV = RandomNumberGenerator.GetBytes(16);

        using var encryptor = aes.CreateEncryptor();
        HMACSHA256? hmac = null;

        var outputStream = new MemoryStream();
        var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);

        // Create an HMAC CryptoStream that wraps the AES CryptoStream.
        if (hmacKey != null)
        {
            hmac = new HMACSHA256(hmacKey);
            cryptoStream = new CryptoStream(cryptoStream, hmac, CryptoStreamMode.Write);
            // Advance the output stream with the hash size, because we want to write the HMAC at position 0 later.
            outputStream.Position += hmac.HashSize / 8;
        }

        try
        {
            await outputStream.WriteAsync(aes.IV).ConfigureAwait(false);
            await inputStream.CopyToAsync(cryptoStream).ConfigureAwait(false);
            await cryptoStream.FlushFinalBlockAsync();

            // Check if we need to write the HMAC to position 0.
            if (hmac != null)
            {
                outputStream.Position = 0;
                await outputStream.WriteAsync(hmac.Hash);
            }

            return outputStream.ToArray();
        }
        finally
        {
            await cryptoStream.DisposeAsync();
            hmac?.Dispose();
        }
    }

    public static async Task<byte[]> SymmetricDecryptAsync(byte[] encrypted, byte[] key, byte[]? hmacKey)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(key));
        }

        if (hmacKey != null && hmacKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(hmacKey));
        }

        byte[]? hash = null;
        var iv = new byte[16];

        HMACSHA256? hmac = null;
        Stream inputStream = new MemoryStream(encrypted);

        try
        {
            if (hmacKey != null)
            {
                hash = new byte[32];
                await inputStream.ReadAsync(hash).ConfigureAwait(false);
                await inputStream.ReadAsync(iv).ConfigureAwait(false);

                hmac = new HMACSHA256(hmacKey);
                inputStream = new CryptoStream(inputStream, hmac, CryptoStreamMode.Read);
            }
            else
            {
                await inputStream.ReadAsync(iv).ConfigureAwait(false);
            }

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            await using var outputStream = new MemoryStream();
            await using var aesStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read);

            await aesStream.CopyToAsync(outputStream).ConfigureAwait(false);

            if (hmac != null)
            {
                var hashesEqual = hmac.Hash!.SequenceEqual(hash!);

                if (hashesEqual)
                {
                    throw new CryptographicException("The HMAC in the input buffer does the match the computed HMAC.");
                }
            }

            return outputStream.ToArray();
        }
        finally
        {
            await inputStream.DisposeAsync();
            hmac?.Dispose();
        }
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
