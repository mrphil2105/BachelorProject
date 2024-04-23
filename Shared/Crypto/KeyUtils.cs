using System.Security.Cryptography;
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
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Apachi.Shared.Crypto
{
    public static class KeyUtils
    {
        public static ReadOnlyMemory<byte> PublicKeyToBytes(ECPublicKeyParameters publicKey)
        {
            var keyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(publicKey);
            var keyBytes = keyInfo.GetEncoded();
            return keyBytes;
        }

        public static ECPublicKeyParameters PublicKeyFromBytes(byte[] keyBytes)
        {
            var keyParameters = (ECPublicKeyParameters)PublicKeyFactory.CreateKey(keyBytes);
            return keyParameters;
        }

        public static AsymmetricCipherKeyPair GenerateKeyPair(string curveName = Constants.DefaultCurveName)
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

        public static byte[] CreateSignature(byte[] data, ECPrivateKeyParameters privateKey)
        {
            var hash = SHA512.HashData(data);
            var signer = new ECDsaSigner();
            signer.Init(true, privateKey);

            var integers = signer.GenerateSignature(hash);
            var signature = DataUtils.SerializeBigIntegers(integers);
            return signature;
        }

        public static bool VerifySignature(byte[] data, byte[] signature, ECPublicKeyParameters publicKey)
        {
            var integers = DataUtils.DeserializeBigIntegers(signature);
            var hash = SHA512.HashData(data);
            var signer = new ECDsaSigner();
            signer.Init(false, publicKey);

            var isValid = signer.VerifySignature(hash, integers[0], integers[1]);
            return isValid;
        }

        public static byte[] AsymmetricEncrypt(
            byte[] data,
            ECPrivateKeyParameters privateKey,
            ECPublicKeyParameters publicKey
        )
        {
            return Crypt(data, privateKey, publicKey, true);
        }

        public static byte[] AsymmetricDecrypt(
            byte[] data,
            ECPrivateKeyParameters privateKey,
            ECPublicKeyParameters publicKey
        )
        {
            return Crypt(data, privateKey, publicKey, false);
        }

        private static byte[] Crypt(
            byte[] data,
            ECPrivateKeyParameters privateKey,
            ECPublicKeyParameters publicKey,
            bool forEncryption
        )
        {
            var engine = new IesEngine(
                new ECDHBasicAgreement(),
                new Kdf2BytesGenerator(new Sha512Digest()),
                new HMac(new Sha512Digest()),
                new PaddedBufferedBlockCipher(new CbcBlockCipher(new AesEngine()))
            );

            var parameters = new IesWithCipherParameters(new byte[64], new byte[64], 128, 256);
            engine.Init(forEncryption, privateKey, publicKey, parameters);
            var encryptedData = engine.ProcessBlock(data, 0, data.Length);
            return encryptedData;
        }
    }
}
