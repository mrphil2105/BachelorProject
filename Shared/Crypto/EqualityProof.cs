using System.Security.Cryptography;
using Org.BouncyCastle.Math;

namespace Apachi.Shared.Crypto;

// Implemented using Schnorr ZKP: https://crypto.stackexchange.com/questions/105725/pedersen-commitments-equivalence
// And also using Schnorr signatures: https://en.wikipedia.org/wiki/Schnorr_signature

public class EqualityProof
{
    private readonly BigInteger _s;
    private readonly BigInteger _e;
    private readonly BigInteger _c;

    private EqualityProof(BigInteger s, BigInteger e, BigInteger c)
    {
        _s = s;
        _e = e;
        _c = c;
    }

    public bool Verify(Commitment c1, Commitment c2)
    {
        // Switch c1 and c2 to negate the value.
        var y = c2.Point.Subtract(c1.Point);
        var rPrime = Commitment.HPoint.Multiply(_s).Add(y.Multiply(_e));

        var rPrimeBytes = rPrime.GetEncoded();
        var cBytes = _c.ToByteArray();
        var toHash = SerializeByteArrays(rPrimeBytes, cBytes);
        var ePrime = new BigInteger(SHA512.HashData(toHash));

        return _e.Equals(ePrime);
    }

    public static EqualityProof Create(BigInteger r1, BigInteger r2)
    {
        var x = r1.Subtract(r2);
        var k = GenerateBigInteger();
        var r = Commitment.HPoint.Multiply(k);
        var c = GenerateBigInteger();

        var rBytes = r.GetEncoded();
        var cBytes = c.ToByteArray();
        var toHash = SerializeByteArrays(rBytes, cBytes);
        var e = new BigInteger(SHA512.HashData(toHash));

        var s = k.Add(x.Multiply(e));
        return new EqualityProof(s, e, c);
    }

    public byte[] ToBytes()
    {
        return SerializeBigIntegers(_s, _e, _c);
    }

    public static EqualityProof FromBytes(byte[] bytes)
    {
        var integers = DeserializeBigIntegers(bytes);
        var s = integers[0];
        var e = integers[1];
        var c = integers[2];
        return new EqualityProof(s, e, c);
    }
}
