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

    public static byte[] GetProgramCommitteePublicKey()
    {
        var publicKeyBase64 = Environment.GetEnvironmentVariable("APACHI_PC_PUBLIC_KEY");

        if (publicKeyBase64 == null)
        {
            throw new InvalidOperationException("Enviroment variable APACHI_PC_PUBLIC_KEY must be set.");
        }

        var publicKey = Convert.FromBase64String(publicKeyBase64);
        return publicKey;
    }

    public static byte[] GetProgramCommitteePrivateKey()
    {
        var privateKeyBase64 = Environment.GetEnvironmentVariable("APACHI_PC_PRIVATE_KEY");

        if (privateKeyBase64 == null)
        {
            throw new InvalidOperationException("Enviroment variable APACHI_PC_PRIVATE_KEY must be set.");
        }

        var privateKey = Convert.FromBase64String(privateKeyBase64);
        return privateKey;
    }
}
