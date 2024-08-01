using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Apachi.Shared.Crypto;

// Implemented using Chaum-pedersen proofs: https://asecuritysite.com/powershell/chaum

public class EqualityProof
{
    private readonly BigInteger _c;
    private readonly BigInteger _s;

    private EqualityProof(BigInteger c, BigInteger s)
    {
        _c = c;
        _s = s;
    }

    // y = g^x, z = h^x
    public bool Verify(ECPoint y, ECPoint z)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        // A' = g^s * y^c
        var aPrime = parameters.G.Multiply(_s).Add(y.Multiply(_c));
        
        // B' = h^s * z^c
        var bPrime = Commitment.HPoint.Multiply(_s).Add(z.Multiply(_c));
        
        // c' = H(g, h, y, z, A', B')
        var hashInput  = SerializeByteArrays(
            parameters.G.GetEncoded(), y.GetEncoded(), Commitment.HPoint.GetEncoded(), z.GetEncoded(), aPrime.GetEncoded(), bPrime.GetEncoded());
        var hashBytes = SHA256.HashData(hashInput);
        var cPrime = new BigInteger(1, hashBytes);
        
        // Compare c and c'
        return _c.Equals(cPrime);
    }

    public byte[] ToBytes()
    {
        return SerializeBigIntegers(_c, _s);
    }

    public static EqualityProof FromBytes(byte[] bytes)
    {
        var integers = DeserializeBigIntegers(bytes);
        var c = integers[0];
        var s = integers[1];
        return new EqualityProof(c, s);
    }

    // private key = x in proof
    public static EqualityProof Create(byte[] privateKey)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var x = new BigInteger(privateKey);

        // generate random value k
        var k = GenerateBigInteger();

        // compute y = g^x, z = h^x
        var y = parameters.G.Multiply(x);
        var z = Commitment.HPoint.Multiply(x);
        
        // compute a = g^k, b = h^k
        var a = parameters.G.Multiply(k);
        var b = Commitment.HPoint.Multiply(k);

        // creating challenge c
        var hashInput = SerializeByteArrays(
            parameters.G.GetEncoded(), y.GetEncoded(), Commitment.HPoint.GetEncoded(), z.GetEncoded(), a.GetEncoded(), b.GetEncoded());
        var hashBytes = SHA256.HashData(hashInput);
        var c = new BigInteger(1, hashBytes);
        
        // compute response s = k - x * c
        var s = k.Subtract(x.Multiply(c));
        
        return new EqualityProof(c, s);
    }
}