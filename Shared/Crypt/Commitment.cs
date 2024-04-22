using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Apachi.Shared.Crypt
{
    public class Commitment
    {
        private const string CurveName = "P-521";

        private readonly ECPoint _point;
        private readonly BigInteger _randomness;

        private Commitment(ECPoint point, BigInteger randomness)
        {
            _point = point;
            _randomness = randomness;
        }

        public static Commitment Create(byte[] value)
        {
            var hash = SHA512.HashData(value);
            var hashInteger = new BigInteger(hash);

            var curve = NistNamedCurves.GetByName(CurveName);
            var domainParameters = new ECDomainParameters(curve);

            var random = new SecureRandom();
            var randomness = new BigInteger(domainParameters.N.BitLength, random);

            var point = domainParameters.G.Multiply(hashInteger).Add(domainParameters.G.Multiply(randomness));
            var commitment = new Commitment(point, randomness);
            return commitment;
        }

        public bool MatchesValue(byte[] value)
        {
            var curve = NistNamedCurves.GetByName(CurveName);
            var domainParameters = new ECDomainParameters(curve);

            var hash = SHA512.HashData(value);
            var hashInteger = new BigInteger(hash);
            var otherPoint = domainParameters.G.Multiply(hashInteger).Add(domainParameters.G.Multiply(_randomness));
            return _point.Equals(otherPoint);
        }

        public byte[] ToBytes()
        {
            var normalizedPoint = _point.Normalize();
            var xCoord = normalizedPoint.AffineXCoord.ToBigInteger();
            var yCoord = normalizedPoint.AffineYCoord.ToBigInteger();
            var commitmentBytes = DataUtils.SerializeBigIntegers(xCoord, yCoord, _randomness);
            return commitmentBytes;
        }

        public static Commitment FromBytes(byte[] bytes)
        {
            var integers = DataUtils.DeserializeBigIntegers(bytes);
            var xCoord = integers[0];
            var yCoord = integers[1];
            var randomness = integers[2];

            var curve = NistNamedCurves.GetByName(CurveName);
            var domainParameters = new ECDomainParameters(curve);

            var point = domainParameters.Curve.CreatePoint(xCoord, yCoord);
            var commitment = new Commitment(point, randomness);
            return commitment;
        }
    }
}
