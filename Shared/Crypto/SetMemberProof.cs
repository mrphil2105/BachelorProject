using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Apachi.Shared.Crypto;

// Zero-knowledge proof (Damgard-Fujisaki method) for a range proof with ECC
// Implemented using: https://asecuritysite.com/zero/z_df5

public class SetMemberProof
{
    private static readonly BigInteger _lowerBound = BigInteger.Zero;

    private readonly ECPoint _c1;
    private readonly ECPoint _c2;

    private SetMemberProof(ECPoint c1, ECPoint c2)
    {
        _c1 = c1;
        _c2 = c2;
    }

    public static SetMemberProof Create(BigInteger index, BigInteger size)
    {
        index = index.Add(BigInteger.One);
        size = size.Add(BigInteger.One);

        var positiveProof = ProofPositive(index);
        var greaterThanProof = ProofPositive(index.Subtract(_lowerBound));
        var lessThanProof = ProofPositive(size.Subtract(index));

        if (!positiveProof.IsPositive || !greaterThanProof.IsPositive || !lessThanProof.IsPositive)
        {
            throw new CryptographicException(
                $"Unable to create proof for {index} within range {_lowerBound} to {size}."
            );
        }

        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        // c1 = (b − x).G − r.H
        var c1 = parameters
            .G.Multiply(size.Subtract(index))
            .Add(Commitment.HPoint.Multiply(positiveProof.Randomness.Negate()));
        // c2 = (x − a).G + r.H
        var c2 = parameters
            .G.Multiply(index.Subtract(_lowerBound))
            .Add(Commitment.HPoint.Multiply(positiveProof.Randomness));

        return new SetMemberProof(c1, c2);
    }

    // x = the index of the element in the set
    public bool Verify(BigInteger size)
    {
        size = size.Add(BigInteger.One);
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        // p1 = b.G - c1
        var p1 = parameters.G.Multiply(size).Subtract(_c1);
        // p2 = a.G + c2
        var p2 = _c2.Add(parameters.G.Multiply(_lowerBound));
        var isValid = p1.Equals(p2);
        return isValid;
    }

    private static (bool IsPositive, BigInteger Randomness) ProofPositive(BigInteger x)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);

        var r0 = DataUtils.GenerateBigInteger();
        var r1 = DataUtils.GenerateBigInteger();
        var r2 = DataUtils.GenerateBigInteger();
        var r3 = DataUtils.GenerateBigInteger();

        var totalR = r0.Add(r1).Add(r2).Add(r3);

        var (x0, x1, x2, x3) = LipmaaDecomp(x);

        if (
            x0.Equals(BigInteger.Zero)
            && x1.Equals(BigInteger.Zero)
            && x2.Equals(BigInteger.Zero)
            && x3.Equals(BigInteger.Zero)
        )
        {
            return (false, BigInteger.Zero);
        }

        var c0 = parameters.G.Multiply(x0.Multiply(x0)).Add(Commitment.HPoint.Multiply(r0));
        var c1 = parameters.G.Multiply(x1.Multiply(x1)).Add(Commitment.HPoint.Multiply(r1));
        var c2 = parameters.G.Multiply(x2.Multiply(x2)).Add(Commitment.HPoint.Multiply(r2));
        var c3 = parameters.G.Multiply(x3.Multiply(x3)).Add(Commitment.HPoint.Multiply(r3));

        // c = c0 + c1 + c2 + c3
        var c = c0.Add(c1).Add(c2).Add(c3);

        // expected = x.G + r.H
        var expected = parameters.G.Multiply(x).Add(Commitment.HPoint.Multiply(totalR));

        return !c.Equals(expected) ? (false, totalR) : (true, totalR);
    }

    public byte[] ToBytes()
    {
        var c1Bytes = _c1.GetEncoded();
        var c2Bytes = _c2.GetEncoded();
        var serialized = SerializeByteArrays(c1Bytes, c2Bytes);
        return serialized;
    }

    public static SetMemberProof FromBytes(byte[] bytes)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        var (c1Bytes, c2Bytes) = DeserializeTwoByteArrays(bytes);
        var c1 = parameters.Curve.DecodePoint(c1Bytes);
        var c2 = parameters.Curve.DecodePoint(c2Bytes);
        return new SetMemberProof(c1, c2);
    }

    private static (BigInteger x0, BigInteger x1, BigInteger x2, BigInteger x3) LipmaaDecomp(BigInteger x)
    {
        var upperBound1 = new BigInteger("100000");
        var upperBound2 = new BigInteger("1000");
        var upperBound3 = new BigInteger("100");
        var upperBound4 = new BigInteger("10");

        if (x.SignValue < 0)
        {
            return (BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero);
        }

        for (var i = BigInteger.Zero; i.CompareTo(upperBound1) < 0; i = i.Add(BigInteger.One))
        {
            for (var j = BigInteger.Zero; j.CompareTo(upperBound2) < 0; j = j.Add(BigInteger.One))
            {
                for (var k = BigInteger.Zero; k.CompareTo(upperBound3) < 0; k = k.Add(BigInteger.One))
                {
                    for (var l = BigInteger.Zero; l.CompareTo(upperBound4) < 0; l = l.Add(BigInteger.One))
                    {
                        // Check if the sum of squares equals the target value
                        var sumOfSquares = i.Pow(2).Add(j.Pow(2)).Add(k.Pow(2)).Add(l.Pow(2));

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
