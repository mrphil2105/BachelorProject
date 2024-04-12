using System.Text;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Paddings;

namespace WebApi.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Parameters;

public class ECCExample
{
    public static void Main(string[] args)
    {
        string ecName = "secp256r1";
        AsymmetricCipherKeyPair keyPair = genKeyPair(ecName);

        ECPrivateKeyParameters privateKey = (ECPrivateKeyParameters) keyPair.Private;
        ECPublicKeyParameters publicKey = (ECPublicKeyParameters) keyPair.Public;

        byte[] data = Encoding.UTF8.GetBytes("Philip er en bitch");
        byte[] encryptedData = EncryptData(data, publicKey, privateKey);
        Console.WriteLine("Encrypted data: " + Convert.ToBase64String(encryptedData));
        
        byte[] decryptedData = DecryptData(encryptedData, privateKey, publicKey);
        Console.WriteLine("Decrypted data: " + Convert.ToBase64String(decryptedData));
        
        //byte[] signData = SignData(data, privateKey);
        // TODO Implement VerifySign
        //bool signatureValid = VerifySign(data, signature, publicKey);

    }
    
    private static AsymmetricCipherKeyPair genKeyPair(string ecName)
    {
        X9ECParameters ec = NistNamedCurves.GetByName(ecName);
        ECDomainParameters domainParameters = new ECDomainParameters(ec.Curve, ec.G, ec.N, ec.H, ec.GetSeed());
        
        ECKeyGenerationParameters keyGenParams =
            new ECKeyGenerationParameters(domainParameters, new SecureRandom());
        
        ECKeyPairGenerator gen = new ECKeyPairGenerator();
        gen.Init(keyGenParams);

        return gen.GenerateKeyPair();
    }

    private static byte[] EncryptData(byte[] data, ECPublicKeyParameters publicKey, ECPrivateKeyParameters privateKey)
    {
        IesEngine iesEngine = new IesEngine(
            new ECDHBasicAgreement(),
            new Kdf2BytesGenerator(new Sha256Digest()),
            new HMac(new Sha256Digest()),
            new BufferedBlockCipher(new AesEngine()));

        IesParameters iesParameters = new IesWithCipherParameters(
            new byte[] { }, new byte[] { }, 128, 256);

        iesEngine.Init(true, privateKey, publicKey, iesParameters);
        return iesEngine.ProcessBlock(data, 0, data.Length);
    }
    
    private static byte[] DecryptData(byte[] data, ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
    {
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
}