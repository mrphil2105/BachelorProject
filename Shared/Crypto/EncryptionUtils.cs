using System.Security.Cryptography;

namespace Apachi.Shared.Crypto;

public static class EncryptionUtils
{
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
