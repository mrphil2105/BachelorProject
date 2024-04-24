using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Apachi.Shared.Crypto;

public static class DataUtils
{
    public static BigInteger GenerateBigInteger(string curveName = Constants.DefaultCurveName)
    {
        var curve = NistNamedCurves.GetByName(curveName);
        var random = new SecureRandom();
        var randomness = new BigInteger(curve.N.BitLength, random);
        return randomness;
    }

    public static byte[] SerializeBigIntegers(params BigInteger[] integers)
    {
        var count = integers.Length;

        if (count > byte.MaxValue)
        {
            throw new ArgumentException($"The integer amount must not exceed {byte.MaxValue}.", nameof(integers));
        }

        var serializedIntegers = integers.Select(integer => integer.ToByteArray()).ToList();
        var totalLength = serializedIntegers.Sum(bytes => bytes.Length);
        var combined = new byte[1 + count + totalLength];
        combined[0] = (byte)count;
        var offset = count + 1;

        for (var i = 0; i < count; i++)
        {
            var serializedInteger = serializedIntegers[i];
            var length = serializedInteger.Length;

            if (length > byte.MaxValue)
            {
                throw new ArgumentException(
                    $"All integers must have a byte length less than {byte.MaxValue}.",
                    nameof(integers)
                );
            }

            combined[i + 1] = (byte)length;
            Buffer.BlockCopy(serializedInteger, 0, combined, offset, length);
            offset += length;
        }

        return combined;
    }

    public static List<BigInteger> DeserializeBigIntegers(byte[] combined)
    {
        var count = combined[0];
        var integers = new List<BigInteger>(count);
        var offset = count + 1;

        for (var i = 0; i < count; i++)
        {
            var length = combined[i + 1];
            var serializedInteger = new byte[length];
            Buffer.BlockCopy(combined, offset, serializedInteger, 0, length);
            var integer = new BigInteger(serializedInteger);
            integers.Add(integer);
            offset += length;
        }

        return integers;
    }

    public static byte[] CombineByteArrays(params byte[][] byteArrays)
    {
        var totalLength = byteArrays.Sum(bytes => bytes.Length);
        using var memoryStream = new MemoryStream(totalLength);

        foreach (var bytes in byteArrays)
        {
            memoryStream.Write(bytes);
        }

        return memoryStream.GetBuffer();
    }
}
