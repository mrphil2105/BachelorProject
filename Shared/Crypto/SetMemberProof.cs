using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;

namespace Apachi.Shared.Crypto;

// Zero-knowledge proof (Damgard-Fujisaki method) for a range proof with ECC
// Implemented using: https://asecuritysite.com/zero/z_df5

public class SetMemberProof
{
    private readonly BigInteger _a;
    private readonly BigInteger _b;

    // G_a = (g, g_L)
    private readonly List<(BigInteger g, BigInteger g_r)> _grades;

    private SetMemberProof(List<(BigInteger g, BigInteger g_r)> grades)
    {
        _grades = grades;
        _a = BigInteger.Zero;
        _b = new BigInteger(grades.Count.ToString()).Add(BigInteger.One);
    }

    public static SetMemberProof Create(List<(BigInteger g, BigInteger g_r)> grades)
    {
        return new SetMemberProof(grades);
    }

    // x = the index of the element in the set
    public bool Verify(BigInteger x)
    {
        var xPlusOne = x.Add(BigInteger.One);

        var positiveCheck = ProofPositive(xPlusOne);

        if (
            !positiveCheck.IsPositive
            || !ProofPositive(xPlusOne.Subtract(_a)).IsPositive
            || !ProofPositive(_b.Subtract(xPlusOne)).IsPositive
        )
        {
            return false;
        }

        var inRange = ProofInRange(xPlusOne, positiveCheck.Randomness);

        return inRange;
    }

    private (bool IsPositive, BigInteger Randomness) ProofPositive(BigInteger x)
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

    private bool ProofInRange(BigInteger x, BigInteger r)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);

        // c1 = (b − x).G − r.H
        var c1 = parameters.G.Multiply(_b.Subtract(x)).Add(Commitment.HPoint.Multiply(r.Negate()));

        // c2 = (x − a).G + r.H
        var c2 = parameters.G.Multiply(x.Subtract(_a)).Add(Commitment.HPoint.Multiply(r));

        // p1 = b.G - c1
        var p1 = parameters.G.Multiply(_b).Subtract(c1);

        // p2 = a.G + c2
        var p2 = c2.Add(parameters.G.Multiply(_a));

        return p1.Equals(p2);
    }

    /* -- DOESN'T WORK --
    public byte[] ToBytes()
    {
        var serializedGradesSet = _gradesList!.SelectMany(grade =>
        {
            var serializedG = DataUtils.SerializeBigIntegers(grade.g);
            var serializedGr = DataUtils.SerializeBigIntegers(grade.g_r);
            return serializedG.Concat(serializedGr).ToArray();
        }).ToArray();

        return DataUtils.SerializeByteArrays(serializedGradesSet);
    }
    */


    /* -- DOESN'T WORK --
    public static SetMemberProof FromBytes(byte[] bytes)
    {
        var serializedGradesSet = DataUtils.DeserializeByteArrays(bytes);
        var gradesList = new List<(BigInteger g, BigInteger g_r)>();
        
        for (var i = 0; i < serializedGradesSet.Count; i += 2)
        {
            var g = new BigInteger(serializedGradesSet[i]);
            var gR = new BigInteger(serializedGradesSet[i + 1]);
            gradesList.Add((g, gR));
        }

        return new SetMemberProof(gradesList);
    }
    */

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

