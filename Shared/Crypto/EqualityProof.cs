using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Apachi.Shared.Crypto;

// Implemented using Schnorr ZKP: https://crypto.stackexchange.com/questions/105725/pedersen-commitments-equivalence

public class EqualityProof
{
    private readonly BigInteger _s; // Schnorr Signature
    private readonly BigInteger _e; // Schnorr Challenge

    private EqualityProof(BigInteger s, BigInteger e)
    {
        _s = s;
        _e = e;
    }

    // y = g^x, z = h^x
    public bool Verify(ECPoint c1, ECPoint c2)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);

        // compute c1 - c2
        var diff = c1.Subtract(c2);

        // compute R = s * G - e * diff
        var r = parameters.G.Multiply(_s).Subtract(diff.Multiply(_e));
        
        // e' = H(diff, R)
        var hashInput  = SerializeByteArrays(diff.GetEncoded(), r.GetEncoded());
        var hashBytes = SHA256.HashData(hashInput);
        var ePrime = new BigInteger(1, hashBytes);
        
        // Compare e' and e
        return _e.Equals(ePrime);
    }

    public byte[] ToBytes()
    {
        return SerializeBigIntegers(_s, _e);
    }

    public static EqualityProof FromBytes(byte[] bytes)
    {
        var integers = DeserializeBigIntegers(bytes);
        var s = integers[0];
        var e = integers[1];
        return new EqualityProof(s, e);
    }

    // b1 and b2 are blinding factors (randomness)
    public static EqualityProof Create(ECPoint c1, ECPoint c2, BigInteger b1, BigInteger b2)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);

        // compute c1 - c2
        var diff = c1.Subtract(c2);
        
        // s = b1 - b2
        var s = b1.Subtract(b2);

        // k = random
        var k = GenerateBigInteger();
        
        // R = k * G
        var r = parameters.G.Multiply(k);
        
        // e = H(diff, R)
        var hashInput = SerializeByteArrays(diff.GetEncoded(), r.GetEncoded());
        var hashBytes = SHA256.HashData(hashInput);
        var e = new BigInteger(1, hashBytes);

        // s' = k + s * e
        var sPrime = k.Add(s.Multiply(e));

        return new EqualityProof(sPrime, e);
    }
}