using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Apachi.Shared.Crypto;

public class Commitment
{
    private const string HPointBase64 =
        "AkJCAYmEWsv4dBo49yya0gRk9+xnUGRRzrSP3oIz30Vrc/dy6tu0lSuxbT4wG0HvyORvpl3jRt4NDCUw9uQstM94XmsYAQCQfxnUJ0Pb9VQ7ZOyW3lai/wfwvjSKN/zvlhUdbPnCvKl1yo4nzbsC0rnepFGQeeh1INu6cXabklrzMzuqd2y4";

    public static readonly ECPoint HPoint;

    private readonly ECPoint _point;

    static Commitment()
    {
        var hBytes = Convert.FromBase64String(HPointBase64);
        var integers = DeserializeBigIntegers(hBytes);
        var xCoord = integers[0];
        var yCoord = integers[1];

        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        HPoint = parameters.Curve.CreatePoint(xCoord, yCoord);
    }

    private Commitment(ECPoint point)
    {
        _point = point;
    }

    public static Commitment Create(byte[] value, BigInteger randomness, string curveName = Constants.DefaultCurveName)
    {
        var hash = SHA512.HashData(value);
        var hashInteger = new BigInteger(hash);

        var parameters = NistNamedCurves.GetByName(curveName);

        var point = parameters.G.Multiply(hashInteger).Add(HPoint.Multiply(randomness));

        var commitment = new Commitment(point);
        return commitment;
    }

    public bool MatchesValue(byte[] value, BigInteger randomness, string curveName = Constants.DefaultCurveName)
    {
        var hash = SHA512.HashData(value);
        var hashInteger = new BigInteger(hash);

        var parameters = NistNamedCurves.GetByName(curveName);
        var otherPoint = parameters.G.Multiply(hashInteger).Add(HPoint.Multiply(randomness));
        return _point.Equals(otherPoint);
    }

    public byte[] ToBytes()
    {
        var normalizedPoint = _point.Normalize();
        var xCoord = normalizedPoint.AffineXCoord.ToBigInteger();
        var yCoord = normalizedPoint.AffineYCoord.ToBigInteger();

        var commitmentBytes = SerializeBigIntegers(xCoord, yCoord);
        return commitmentBytes;
    }

    public static Commitment FromBytes(byte[] bytes, string curveName = Constants.DefaultCurveName)
    {
        var integers = DeserializeBigIntegers(bytes);
        var xCoord = integers[0];
        var yCoord = integers[1];

        var parameters = NistNamedCurves.GetByName(curveName);
        var point = parameters.Curve.CreatePoint(xCoord, yCoord);

        var commitment = new Commitment(point);
        return commitment;
    }
}
