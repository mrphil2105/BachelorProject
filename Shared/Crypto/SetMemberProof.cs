using Org.BouncyCastle.Math;

namespace Apachi.Shared.Crypto;

// Range proof that a value is in a set
// Implemented using: https://asecuritysite.com/zero/range02

public class SetMemberProof
{
    private readonly BigInteger _a;
    private readonly BigInteger _b;
    private readonly List<int> grades = [2, 4, 7, 10, 12];

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

    private static (BigInteger, BigInteger, BigInteger, BigInteger) LipmaaDecomp(BigInteger x)
    {
        BigInteger upperBound1 = new BigInteger("100000");
        BigInteger upperBound2 = new BigInteger("1000");
        BigInteger upperBound3 = new BigInteger("100");
        BigInteger upperBound4 = new BigInteger("10");

        for (BigInteger i = BigInteger.Zero; i.CompareTo(upperBound1) < 0; i = i.Add(BigInteger.One))
        {
            for (BigInteger j = BigInteger.Zero; j.CompareTo(upperBound2) < 0; j = j.Add(BigInteger.One))
            {
                for (BigInteger k = BigInteger.Zero; k.CompareTo(upperBound3) < 0; k = k.Add(BigInteger.One))
                {
                    for (BigInteger l = BigInteger.Zero; l.CompareTo(upperBound4) < 0; l = l.Add(BigInteger.One))
                    {
                        BigInteger sumOfSquares = i.Pow(2).Add(j.Pow(2)).Add(k.Pow(2)).Add(l.Pow(2));
                        if (sumOfSquares.Equals(x))
                        {
                            return (i, j, k, l);
                        }
                    }
                }
            }
        }

        return (BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero);
    }
}