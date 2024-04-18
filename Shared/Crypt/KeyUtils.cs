using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Apachi.Shared.Crypt;

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
}
