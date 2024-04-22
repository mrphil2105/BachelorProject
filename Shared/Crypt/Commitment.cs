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
        private readonly ECPoint _point;
        private readonly BigInteger _randomness;
        private const string CurveName = "P-521";

        private Commitment(ECPoint point, BigInteger randomness)
        {
            _point = point;
            _randomness = randomness;
        }

        public static Commitment Create(byte[] value)
        {
            var hashedValue = SHA512.HashData(value);
            var bigVal = new BigInteger(hashedValue);

            var ec = NistNamedCurves.GetByName(CurveName);
            var domainParameters = new ECDomainParameters(ec);

            var random = new SecureRandom();
            var randomness = new BigInteger(domainParameters.N.BitLength, random);
            var commitment = domainParameters.G.Multiply(bigVal).Add(domainParameters.G.Multiply(randomness));

            return new Commitment(commitment, randomness);
        }

        public byte[] ToBytes()
        {
            var normalizedPoint = _point.Normalize();
            var xCoord = normalizedPoint.AffineXCoord.ToBigInteger().ToByteArray();
            var yCoord = normalizedPoint.AffineYCoord.ToBigInteger().ToByteArray();
            var rand = _randomness.ToByteArray();

            var serializedBytes = new byte[2 + xCoord.Length + yCoord.Length + rand.Length];
            serializedBytes[0] = (byte)xCoord.Length;
            serializedBytes[1] = (byte)yCoord.Length;

            var intOffset = 2;

            Buffer.BlockCopy(xCoord, 0, serializedBytes, intOffset, xCoord.Length);

            intOffset += xCoord.Length;

            Buffer.BlockCopy(yCoord, 0, serializedBytes, intOffset, yCoord.Length);

            intOffset += yCoord.Length;

            Buffer.BlockCopy(rand, 0, serializedBytes, intOffset, rand.Length);

            return serializedBytes;
        }

        public static Commitment FromBytes(byte[] bytes)
        {
            var xLength = bytes[0];
            var yLength = bytes[1];
            var xCoord = new byte[xLength];
            var yCoord = new byte[yLength];

            var intOffset = 2;

            Buffer.BlockCopy(bytes, intOffset, xCoord, 0, xLength);

            intOffset += xLength;

            Buffer.BlockCopy(bytes, intOffset, yCoord, 0, yLength);

            intOffset += yLength;

            var rand = new byte[bytes.Length - intOffset];

            Buffer.BlockCopy(bytes, intOffset, rand, 0, rand.Length);

            var ec = NistNamedCurves.GetByName(CurveName);
            var domainParameters = new ECDomainParameters(ec);

            var x = new BigInteger(xCoord);
            var y = new BigInteger(yCoord);

            var point = domainParameters.Curve.CreatePoint(x, y);
            var randomness = new BigInteger(rand);

            return new Commitment(point, randomness);
        }

        public bool MatchesValue(byte[] value)
        {
            var ec = NistNamedCurves.GetByName(CurveName);
            var domainParameters = new ECDomainParameters(ec.Curve, ec.G, ec.N, ec.H, ec.GetSeed());

            var hashedValue = SHA512.HashData(value);
            var bigVal = new BigInteger(hashedValue);

            var check = domainParameters.G.Multiply(bigVal).Add(domainParameters.G.Multiply(_randomness));

            return _point.Equals(check);
        }
    }
}
