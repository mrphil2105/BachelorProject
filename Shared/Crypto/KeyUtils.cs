using System.Security.Cryptography;

namespace Apachi.Shared.Crypto;

public static class KeyUtils
{
    public static (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
    {
        using var rsa = RSA.Create(Constants.DefaultRSAKeySize);
        var publicKey = rsa.ExportRSAPublicKey();
        var privateKey = rsa.ExportRSAPrivateKey();
        return (publicKey, privateKey);
    }

    public static byte[] CalculateSignature(byte[] data, byte[] privateKey)
    {
        using var rsa = RSA.Create(Constants.DefaultRSAKeySize);
        rsa.ImportRSAPrivateKey(privateKey, out _);
        var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return signature;
    }

    public static bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
    {
        using var rsa = RSA.Create(Constants.DefaultRSAKeySize);
        rsa.ImportRSAPublicKey(publicKey, out _);
        var isValid = rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        return isValid;
    }
}
