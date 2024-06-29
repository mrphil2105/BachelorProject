using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Apachi.Shared.Crypto;

public static class EncryptionUtils
{
    public static async Task<byte[]> SymmetricEncryptAsync(byte[] value, byte[] aesKey, byte[]? hmacKey)
    {
        await using var inputStream = new MemoryStream(value);
        return await SymmetricEncryptAsync(inputStream, aesKey, hmacKey);
    }

    public static async Task<byte[]> SymmetricEncryptAsync(Stream inputStream, byte[] aesKey, byte[]? hmacKey)
    {
        await using var outputStream = new MemoryStream();
        await SymmetricEncryptAsync(inputStream, outputStream, aesKey, hmacKey);
        return outputStream.ToArray();
    }

    public static async Task SymmetricEncryptAsync(byte[] value, Stream outputStream, byte[] aesKey, byte[]? hmacKey)
    {
        await using var inputStream = new MemoryStream(value);
        await SymmetricEncryptAsync(inputStream, outputStream, aesKey, hmacKey);
    }

    public static async Task SymmetricEncryptAsync(
        Stream inputStream,
        Stream outputStream,
        byte[] aesKey,
        byte[]? hmacKey
    )
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

        var initialPosition = outputStream.Position;
        var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write, true);

        // Create an HMAC CryptoStream that wraps the AES CryptoStream.
        if (hmacKey != null)
        {
            hmac = new HMACSHA256(hmacKey);
            cryptoStream = new CryptoStream(cryptoStream, hmac, CryptoStreamMode.Write);
            // Advance the output stream with the hash size, because we want to write the HMAC at initial position.
            outputStream.Position += hmac.HashSize / 8;
        }

        try
        {
            await outputStream.WriteAsync(aes.IV);
            await inputStream.CopyToAsync(cryptoStream);
            await cryptoStream.FlushFinalBlockAsync();

            // Check if we need to write the HMAC to initial position.
            if (hmac != null)
            {
                outputStream.Position = initialPosition;
                await outputStream.WriteAsync(hmac.Hash);
            }
        }
        finally
        {
            await cryptoStream.DisposeAsync();
            hmac?.Dispose();
        }
    }

    public static async Task<byte[]> SymmetricDecryptAsync(byte[] encrypted, byte[] aesKey, byte[]? hmacKey)
    {
        await using var inputStream = new MemoryStream(encrypted);
        return await SymmetricDecryptAsync(inputStream, aesKey, hmacKey);
    }

    public static async Task<byte[]> SymmetricDecryptAsync(Stream inputStream, byte[] aesKey, byte[]? hmacKey)
    {
        await using var outputStream = new MemoryStream();
        await SymmetricDecryptAsync(inputStream, outputStream, aesKey, hmacKey);
        return outputStream.ToArray();
    }

    public static async Task SymmetricDecryptAsync(
        byte[] encrypted,
        Stream outputStream,
        byte[] aesKey,
        byte[]? hmacKey
    )
    {
        await using var inputStream = new MemoryStream(encrypted);
        await SymmetricDecryptAsync(inputStream, outputStream, aesKey, hmacKey);
    }

    public static async Task SymmetricDecryptAsync(
        Stream inputStream,
        Stream outputStream,
        byte[] aesKey,
        byte[]? hmacKey
    )
    {
        if (aesKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(aesKey));
        }

        if (hmacKey != null && hmacKey.Length != 32)
        {
            throw new ArgumentException("The key must be 256 bits long.", nameof(hmacKey));
        }

        byte[]? hash = null;
        var iv = new byte[16];

        HMACSHA256? hmac = null;
        CryptoStream? hmacStream = null;

        try
        {
            if (hmacKey != null)
            {
                hash = new byte[32];
                await inputStream.ReadAsync(hash);
                await inputStream.ReadAsync(iv);

                hmac = new HMACSHA256(hmacKey);
                hmacStream = new CryptoStream(inputStream, hmac, CryptoStreamMode.Read, true);
            }
            else
            {
                await inputStream.ReadAsync(iv);
            }

            using var aes = Aes.Create();
            aes.Key = aesKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            await using var aesStream = new CryptoStream(
                hmacStream ?? inputStream,
                decryptor,
                CryptoStreamMode.Read,
                true
            );

            await aesStream.CopyToAsync(outputStream);

            if (hmac != null)
            {
                var hashesEqual = hmac.Hash!.SequenceEqual(hash!);

                if (hashesEqual)
                {
                    throw new CryptographicException("The HMAC in the input buffer does the match the computed HMAC.");
                }
            }
        }
        finally
        {
            if (hmacStream != null)
            {
                await hmacStream.DisposeAsync();
            }

            hmac?.Dispose();
        }
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

    public static async Task<byte[]> AsymmetricLargeEncryptAsync(byte[] value, byte[] publicKey)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var encryptedKey = await AsymmetricEncryptAsync(key, publicKey);

        var lengthPrefix = new byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(lengthPrefix, (ushort)encryptedKey.Length);

        await using var outputStream = new MemoryStream();
        await outputStream.WriteAsync(lengthPrefix);
        await outputStream.WriteAsync(encryptedKey);
        await SymmetricEncryptAsync(value, outputStream, key, null);

        return outputStream.ToArray();
    }

    public static async Task<byte[]> AsymmetricLargeDecryptAsync(byte[] value, byte[] privateKey)
    {
        await using var inputStream = new MemoryStream(value);

        var lengthPrefix = new byte[sizeof(ushort)];
        await inputStream.ReadAsync(lengthPrefix);
        var encryptedKeyLength = BinaryPrimitives.ReadUInt16BigEndian(lengthPrefix);

        var encryptedKey = new byte[encryptedKeyLength];
        await inputStream.ReadAsync(encryptedKey);

        var key = await AsymmetricDecryptAsync(encryptedKey, privateKey);
        return await SymmetricDecryptAsync(inputStream, key, null);
    }
}
