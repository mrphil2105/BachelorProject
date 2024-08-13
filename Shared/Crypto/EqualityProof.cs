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
    public bool Verify(Commitment c1, Commitment c2)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);

        // compute c1 - c2
        var diff = c1.Point.Subtract(c2.Point);

        // compute R = s * G - e * diff
        var r = Commitment.HPoint.Multiply(_s).Subtract(diff.Multiply(_e));
        
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
    public static EqualityProof Create(byte[] value, BigInteger r1, BigInteger r2)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var hash = SHA512.HashData(value);
        var hashInteger = new BigInteger(hash);
        
        var c1 = parameters.G.Multiply(hashInteger).Add(Commitment.HPoint.Multiply(r1));
        var c2 = parameters.G.Multiply(hashInteger).Add(Commitment.HPoint.Multiply(r2));

        // compute c1 - c2
        var diff = c1.Subtract(c2);
        
        // s = b1 - b2
        var s = r1.Subtract(r2);

        // k = random
        var k = GenerateBigInteger();
        
        // R = k * G
        var r = Commitment.HPoint.Multiply(k);
        
        // e = H(diff, R)
        var hashInput = SerializeByteArrays(diff.GetEncoded(), r.GetEncoded());
        var hashBytes = SHA256.HashData(hashInput);
        var e = new BigInteger(1, hashBytes);

        // s' = k + s * e
        var sPrime = k.Add(s.Multiply(e));

        return new EqualityProof(sPrime, e);
    }
}