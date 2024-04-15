using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;

namespace WebApi.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

public class ECCExample
{
    public static void Main()
    {
        string ecName = "secp256r1";
        byte[] data = "Philip er en bitch"u8.ToArray();

        AsymmetricCipherKeyPair keyPair = GenKeyPair(ecName);
        ECPrivateKeyParameters privateKey = (ECPrivateKeyParameters)keyPair.Private;

        // Unnecessary for encryption and decryption
        ECPublicKeyParameters publicKey = (ECPublicKeyParameters)keyPair.Public;

        using (Aes aes = Aes.Create())
        {
            byte[] encryptedData = EncryptData(data, privateKey, aes.IV);
            Console.WriteLine("Encrypted data: " + Convert.ToBase64String(encryptedData));

            byte[] decryptedData = DecryptData(encryptedData, privateKey, aes.IV);
            Console.WriteLine("Decrypted data: " + Convert.ToBase64String(decryptedData));
        }

        //byte[] signData = SignData(data, privateKey);
        // TODO Implement VerifySign
        //bool signatureValid = VerifySign(data, signature, publicKey);

    }

    private static AsymmetricCipherKeyPair GenKeyPair(string ecName)
    {
        X9ECParameters ec = NistNamedCurves.GetByName(ecName);
        ECDomainParameters domainParameters = new ECDomainParameters(ec.Curve, ec.G, ec.N, ec.H, ec.GetSeed());

        ECKeyGenerationParameters keyGenParams =
            new ECKeyGenerationParameters(domainParameters, new SecureRandom());

        ECKeyPairGenerator gen = new ECKeyPairGenerator();
        gen.Init(keyGenParams);

        return gen.GenerateKeyPair();
    }

    private static byte[] EncryptData(byte[] data, ECPrivateKeyParameters privateKey, byte[] IV)
    {
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("data");
        if (privateKey == null || data.Length <= 0)
            throw new ArgumentNullException("privateKey");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("IV");
        
        /*
        IesEngine iesEngine = new IesEngine(
            new ECDHBasicAgreement(),
            new Kdf2BytesGenerator(new Sha256Digest()),
            new HMac(new Sha256Digest()),
            new BufferedBlockCipher(new AesEngine()));

        IesParameters iesParameters = new IesWithCipherParameters(
            new byte[] { }, new byte[] { }, 128, 256);

        iesEngine.Init(true, privateKey, publicKey, iesParameters);
        return iesEngine.ProcessBlock(data, 0, data.Length);
        */

        byte[] encrypted;

        using (Aes aes = Aes.Create())
        {
            aes.Key = privateKey.D.ToByteArrayUnsigned();
            aes.IV = IV;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                }

                encrypted = ms.ToArray();
            }
        }

        return encrypted;
    }

    private static byte[] DecryptData(byte[] data, ECPrivateKeyParameters privateKey, byte[] IV)
    {
        /*
        IesEngine iesEngine = new IesEngine(
            new ECDHBasicAgreement(),
            new Kdf2BytesGenerator(new Sha256Digest()),
            new HMac(new Sha256Digest()),
            new BufferedBlockCipher(new AesEngine()));

        IesParameters iesParameters = new IesWithCipherParameters(
            new byte[] {}, new byte[] {}, 128, 256);

        iesEngine.Init(false, privateKey, publicKey, iesParameters);
        return iesEngine.ProcessBlock(data, 0, data.Length);
    }
        */
        
        if (data == null || data.Length <= 0)
            throw new ArgumentNullException("data");
        if (privateKey == null || data.Length <= 0)
            throw new ArgumentNullException("privateKey");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("IV");

        byte[] decrypted;

        using (Aes aes = Aes.Create())
        {
            aes.Key = privateKey.D.ToByteArrayUnsigned();
            aes.IV = IV;
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream(data))
            {
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    cs.CopyTo(ms);
                }

                decrypted = ms.ToArray();
            }
        }

        return decrypted;
    }
}