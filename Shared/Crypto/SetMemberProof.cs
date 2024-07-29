using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace Apachi.Shared.Crypto;

// Range proof that a value is in a set
// Implemented using: https://asecuritysite.com/zero/range02

public class SetMemberProof
{
    private readonly BigInteger _a;
    private readonly BigInteger _b;
    
    public SetMemberProof(BigInteger a, BigInteger b)
    {
        _a = a;
        _b = b;
    }

    public bool Verify(Commitment commitment, BigInteger randomness, BigInteger x)
    {
        
        bool isInRange = x.CompareTo(_a) >= 0 &&
                         x.CompareTo(_b) <= 0;
        
        bool matchesValue = commitment.MatchesValue(x.ToByteArray(), randomness);
        
        return isInRange && matchesValue;
    }
    
    public static SetMemberProof Create(BigInteger a, BigInteger b)
    {
        return new SetMemberProof(a, b);
    }
    
    public byte[] ToBytes()
    {
        return DataUtils.SerializeBigIntegers(_a, _b);
    }
    
    public static SetMemberProof FromBytes(byte[] bytes)
    {
        var integers = DataUtils.DeserializeBigIntegers(bytes);
        return new SetMemberProof(integers[0], integers[1]);
    }

    private static BigInteger[] DecomposeIntoPowersOf2(BigInteger x)
    {
        var x0 = x.And(BigInteger.One);
        var x1 = x.And(BigInteger.Two);
        var x2 = x.And(BigInteger.One.ShiftLeft(2));
        var x3 = x.And(BigInteger.One.ShiftLeft(3));

        return [x0, x1, x2, x3];
    }
}