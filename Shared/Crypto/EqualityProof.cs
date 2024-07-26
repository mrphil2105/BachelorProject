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

    public EqualityProof(BigInteger c, BigInteger s)
    {
        _c = c;
        _s = s;
    }

    public bool Verify(ECPoint g, ECPoint h, ECPoint y, ECPoint z, string curveName = Constants.DefaultCurveName)
    {
        var parameters = NistNamedCurves.GetByName(curveName);
        var curveOrder = parameters.N;
        
        // A' = g^s * y^c
        var aPrime = g.Multiply(_s).Add(y.Multiply(_c));
        
        // B' = h^s * z^c
        var bPrime = h.Multiply(_s).Add(z.Multiply(_c));
        
        // c' = H(g, h, y, z, A', B')
        var hashInput  = DataUtils.SerializeByteArrays(
            g.GetEncoded(), y.GetEncoded(), h.GetEncoded(), z.GetEncoded(), aPrime.GetEncoded(), bPrime.GetEncoded());
        var hashBytes = SHA256.HashData(hashInput);
        var cPrime = new BigInteger(1, hashBytes).Mod(curveOrder);
        
        // Compare c and c'
        return _c.Equals(cPrime);
    }

    public byte[] ToBytes()
    {
        return DataUtils.SerializeBigIntegers(_c, _s);
    }

    public static EqualityProof FromBytes(byte[] bytes)
    {
        var integers = DataUtils.DeserializeBigIntegers(bytes);
        var c = integers[0];
        var s = integers[1];
        return new EqualityProof(c, s);
    }

    public static EqualityProof Create(ECPoint g, ECPoint h, BigInteger x, string curveName = Constants.DefaultCurveName)
    {
        var parameters = NistNamedCurves.GetByName(curveName);
        var curveOrder = parameters.N;

        // generate random valeu k
        var k = DataUtils.GenerateBigInteger(curveName);

        // compute y = g^x, z = h^x
        var y = g.Multiply(x);
        var z = h.Multiply(x);
        
        // compute a = g^k, b = h^k
        var a = g.Multiply(k);
        var b = h.Multiply(k);

        // creating challenge c
        var hashInput = DataUtils.SerializeByteArrays(
            g.GetEncoded(), y.GetEncoded(), h.GetEncoded(), z.GetEncoded(), a.GetEncoded(), b.GetEncoded());
        var hashBytes = SHA256.HashData(hashInput);
        var c = new BigInteger(1, hashBytes).Mod(curveOrder);
        
        // compute response s = k - x * c
        var s = k.Subtract(x.Multiply(c)).Mod(curveOrder);
        
        return new EqualityProof(c, s);
    }
}