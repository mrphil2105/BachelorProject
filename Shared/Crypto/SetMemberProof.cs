using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;


namespace Apachi.Shared.Crypto;

// Zero-knowledge proof (Damgard-Fujisaki method) for a range proof with ECC
// Implemented using: https://asecuritysite.com/zero/z_df5

public class SetMemberProof
{
    private static readonly BigInteger[] Grades =
    {
        BigInteger.Two,
        BigInteger.Four,
        new("7"),
        BigInteger.Ten, 
        new("12")
    };
    
    private readonly BigInteger _a = BigInteger.Zero;
    private readonly BigInteger _b = new(Grades.Length.ToString());

    // x = the grade
    public bool Verify(BigInteger x)
    {
        var xPlusOne = x.Add(BigInteger.One);
        
        var positiveCheck = IsPositive(xPlusOne);
        
        if (!positiveCheck.Item1 || !IsPositive(xPlusOne.Subtract(_a)).Item1 || !IsPositive(_b.Subtract(xPlusOne)).Item1)
        {
            return false;
        }
        
        return IsInRange(xPlusOne, positiveCheck.Item2);
    }

    public static SetMemberProof Create()
    {
        return new SetMemberProof();
    }
    
    private (bool, BigInteger) IsPositive (BigInteger nv)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var r0 = DataUtils.GenerateBigInteger();
        var r1 = DataUtils.GenerateBigInteger();
        var r2 = DataUtils.GenerateBigInteger();
        var r3 = DataUtils.GenerateBigInteger();
        
        var totalR = r0.Add(r1).Add(r2).Add(r3);

        var n = new BigInteger(1, nv.ToByteArray()).Mod(parameters.N);
        
        var (x0, x1, x2, x3) = LipmaaDecomp(nv);
        if (x0.Equals(BigInteger.Zero) && x1.Equals(BigInteger.Zero) && x2.Equals(BigInteger.Zero) && x3.Equals(BigInteger.Zero))
        {
            return (false, BigInteger.Zero);
        }

        // x0.multiply(x0)
        // r0
        
        // use commitment class? 
        var c0 = parameters.G.Multiply(x0.Multiply(x0)).Add(Commitment.HPoint.Multiply(r0));
        var c1 = parameters.G.Multiply(x1.Multiply(x1)).Add(Commitment.HPoint.Multiply(r1));
        var c2 = parameters.G.Multiply(x2.Multiply(x2)).Add(Commitment.HPoint.Multiply(r2));
        var c3 = parameters.G.Multiply(x3.Multiply(x3)).Add(Commitment.HPoint.Multiply(r3));

        // c
        var combinedCommitment = c0.Add(c1).Add(c2).Add(c3);
        
        // val
        var expected = parameters.G.Multiply(n).Add(Commitment.HPoint.Multiply(totalR));

        return !combinedCommitment.Equals(expected) ? (false, totalR) : (true, totalR);
    }
    
    private bool IsInRange (BigInteger xv, BigInteger r)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        // to commitment class
        var c1 = parameters.G.Multiply(_b.Subtract(xv)).Add(Commitment.HPoint.Multiply(r.Negate()));
        var c2 = parameters.G.Multiply(xv.Subtract(_a)).Add(Commitment.HPoint.Multiply(r));
        
        var p1 = parameters.G.Multiply(_b).Subtract(c1);
        var p2 = c2.Add(parameters.G.Multiply(_a));
        
        return p1.Equals(p2);
    }


    /*
    public byte[] ToBytes()
    {
        return DataUtils.SerializeBigIntegers(_a, _b);
    }

    
    public static SetMemberProof FromBytes(byte[] bytes)
    {
        var integers = DataUtils.DeserializeBigIntegers(bytes);
        var a = new BigInteger(integers[0].ToString());
        var b = new BigInteger(integers[1]);

        var proof = new SetMemberProof(a, b);
        return proof;
    }
    */

    public static (BigInteger, BigInteger, BigInteger, BigInteger) LipmaaDecomp(BigInteger x)
    {
        BigInteger upperBound1 = new BigInteger("100000");
        BigInteger upperBound2 = new BigInteger("1000");
        BigInteger upperBound3 = new BigInteger("100");
        BigInteger upperBound4 = new BigInteger("10");

        if (x.SignValue < 0)
        {
            return (BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero);
        }

        for (BigInteger i = BigInteger.Zero; i.CompareTo(upperBound1) < 0; i = i.Add(BigInteger.One))
        {
            for (BigInteger j = BigInteger.Zero; j.CompareTo(upperBound2) < 0; j = j.Add(BigInteger.One))
            {
                for (BigInteger k = BigInteger.Zero; k.CompareTo(upperBound3) < 0; k = k.Add(BigInteger.One))
                {
                    for (BigInteger l = BigInteger.Zero; l.CompareTo(upperBound4) < 0; l = l.Add(BigInteger.One))
                    {
                        // Check if the sum of squares equals the target value
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