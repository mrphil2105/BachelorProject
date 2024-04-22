using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Apachi.Shared.Crypt
{
    public static class KeyUtils
    {
        public static AsymmetricCipherKeyPair GenerateKeyPair(string curveName = "P-521")
        {
            var curveParameters = NistNamedCurves.GetByName(curveName);
            var domainParameters = new ECDomainParameters(curveParameters);
            var random = new SecureRandom();
            var generationParameters = new ECKeyGenerationParameters(domainParameters, random);
            var generator = new ECKeyPairGenerator();
            generator.Init(generationParameters);
            var keyPair = generator.GenerateKeyPair();
            return keyPair;
        }
        
        public static (BigInteger point, BigInteger signature) CreateSignature(byte[] data, ECPrivateKeyParameters privateKey)
        {
            var hashedData = SHA512.HashData(data);
            var signer = new ECDsaSigner();
            signer.Init(true, privateKey);
            
            var signature = signer.GenerateSignature(hashedData);
            
            return (signature[0], signature[1]);
        }
        
        public static bool VerifySignature(byte[] data, (BigInteger point, BigInteger signature) signature, ECPublicKeyParameters publicKey)
        {
            var hashedData = SHA512.HashData(data);
            var signer = new ECDsaSigner();
            signer.Init(false, publicKey);
            
            return signer.VerifySignature(hashedData, signature.point, signature.signature);
        }

        private static byte[] Crypt(bool forEncryption, byte[] data, ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
        {
            IesEngine engine = new IesEngine(
                new ECDHBasicAgreement(),
                new Kdf2BytesGenerator(new Sha512Digest()),
                new HMac(new Sha512Digest()),
                new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine())));

            IesParameters parameters = new IesWithCipherParameters(new byte[64], new byte[64], 128, 256);
            engine.Init(forEncryption, privateKey, publicKey, parameters);
            
            byte[] encryptedData = engine.ProcessBlock(data, 0, data.Length);

            return encryptedData;
        }
        
        public static byte[] AsymmetricEncrypt (byte[] data, ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
        {
            return Crypt(true, data, privateKey, publicKey);
        }
        
        public static byte[] AsymmetricDecrypt (byte[] data, ECPrivateKeyParameters privateKey, ECPublicKeyParameters publicKey)
        {
            return Crypt(false, data, privateKey, publicKey);
        }

        public static ReadOnlyMemory<byte> PublicKeyToBytes(ECPublicKeyParameters publicKey)
        {
            var keyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
            var keyBytes = keyInfo.GetEncoded();
            return keyBytes;
        }

        public static ECPublicKeyParameters PublicKeyFromBytes(ReadOnlyMemory<byte> keyBytes)
        {
            var keyByteArray = keyBytes.ToArray();
            var keyParameters = (ECPublicKeyParameters)PublicKeyFactory.CreateKey(keyByteArray);
            return keyParameters;
        }
    }
}
