using System.Buffers.Binary;
using System.Runtime.Serialization;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Apachi.Shared.Crypto;

public static class DataUtils
{
    private const int MaxByteArrayLength = 50 * 1024 * 1024; // 50 MiB

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
        var serialized = new byte[1 + count + totalLength];
        serialized[0] = (byte)count;
        var offset = count + 1;

        for (var i = 0; i < count; i++)
        {
            var serializedInteger = serializedIntegers[i];
            var length = serializedInteger.Length;

            if (length > byte.MaxValue)
            {
                throw new SerializationException($"All integers must have a byte length less than {byte.MaxValue}.");
            }

            serialized[i + 1] = (byte)length;
            Buffer.BlockCopy(serializedInteger, 0, serialized, offset, length);
            offset += length;
        }

        return serialized;
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

    public static byte[] SerializeByteArrays(params byte[][] byteArrays)
    {
        return SerializeByteArrays((IEnumerable<byte[]>)byteArrays);
    }

    public static byte[] SerializeByteArrays(IEnumerable<byte[]> byteArrays)
    {
        if (byteArrays is not ICollection<byte[]> collection)
        {
            collection = byteArrays.ToList();
        }

        var count = collection.Count;

        if (count > ushort.MaxValue)
        {
            throw new ArgumentException(
                $"The amount of byte arrays must not exceed {ushort.MaxValue}.",
                nameof(byteArrays)
            );
        }

        var totalLength = collection.Sum(array => array.Length);
        var capacity = totalLength + count * sizeof(int) + sizeof(ushort);
        using var memoryStream = new MemoryStream(capacity);

        Span<byte> countPrefix = stackalloc byte[sizeof(ushort)];
        BinaryPrimitives.WriteUInt16BigEndian(countPrefix, (ushort)count);
        memoryStream.Write(countPrefix);

        Span<byte> lengthPrefix = stackalloc byte[sizeof(int)];

        foreach (var array in collection)
        {
            if (array.Length > MaxByteArrayLength)
            {
                throw new SerializationException($"All byte arrays must not be longer than {MaxByteArrayLength}.");
            }

            BinaryPrimitives.WriteInt32BigEndian(lengthPrefix, array.Length);
            memoryStream.Write(lengthPrefix);
            memoryStream.Write(array);
        }

        return memoryStream.GetBuffer();
    }

    public static List<byte[]> DeserializeByteArrays(byte[] serialized)
    {
        using var memoryStream = new MemoryStream(serialized);

        Span<byte> countPrefix = stackalloc byte[sizeof(ushort)];
        memoryStream.Read(countPrefix);
        var count = BinaryPrimitives.ReadUInt16BigEndian(countPrefix);

        Span<byte> lengthPrefix = stackalloc byte[sizeof(int)];
        var byteArrays = new List<byte[]>();

        for (var i = 0; i < count; i++)
        {
            memoryStream.Read(lengthPrefix);
            var length = BinaryPrimitives.ReadInt32BigEndian(lengthPrefix);

            if (length > MaxByteArrayLength)
            {
                throw new SerializationException(
                    $"Serialized data contains byte array longer than {MaxByteArrayLength}."
                );
            }

            var array = new byte[length];
            var bytesRead = memoryStream.Read(array);

            if (bytesRead != length)
            {
                throw new SerializationException(
                    "Serialized data contains incomplete byte array or incorrect length prefix."
                );
            }

            byteArrays.Add(array);
        }

        return byteArrays;
    }

    public static byte[] DeserializeOneByteArray(byte[] serialized)
    {
        var byteArrays = DeserializeByteArrays(serialized);

        if (byteArrays.Count == 0)
        {
            throw new SerializationException("Serialized data must contain at least one byte array.");
        }

        return byteArrays[0];
    }

    public static (byte[] First, byte[] Second) DeserializeTwoByteArrays(byte[] serialized)
    {
        var byteArrays = DeserializeByteArrays(serialized);

        if (byteArrays.Count < 2)
        {
            throw new SerializationException("Serialized data must contain at least two byte arrays.");
        }

        return (byteArrays[0], byteArrays[1]);
    }

    public static (byte[] First, byte[] Second, byte[] Third) DeserializeThreeByteArrays(byte[] serialized)
    {
        var byteArrays = DeserializeByteArrays(serialized);

        if (byteArrays.Count < 3)
        {
            throw new SerializationException("Serialized data must contain at least three byte arrays.");
        }

        return (byteArrays[0], byteArrays[1], byteArrays[2]);
    }

    public static (byte[] First, byte[] Second, byte[] Third, byte[] Fourth) DeserializeFourByteArrays(
        byte[] serialized
    )
    {
        var byteArrays = DeserializeByteArrays(serialized);

        if (byteArrays.Count < 4)
        {
            throw new SerializationException("Serialized data must contain at least four byte arrays.");
        }

        return (byteArrays[0], byteArrays[1], byteArrays[2], byteArrays[3]);
    }

    public static (byte[] First, byte[] Second, byte[] Third, byte[] Fourth, byte[] Fifth) DeserializeFiveByteArrays(
        byte[] serialized
    )
    {
        var byteArrays = DeserializeByteArrays(serialized);

        if (byteArrays.Count < 5)
        {
            throw new SerializationException("Serialized data must contain at least five byte arrays.");
        }

        return (byteArrays[0], byteArrays[1], byteArrays[2], byteArrays[3], byteArrays[4]);
    }

    public static byte[] SerializeGrade(int grade, byte[] gradeNonce)
    {
        var gradeByte = (byte)(grade + 3);
        var serialized = SerializeByteArrays(new byte[] { gradeByte }, gradeNonce);
        return serialized;
    }

    public static (int Grade, byte[] GradeNonce) DeserializeGrade(byte[] serialized)
    {
        var (gradeBytes, gradeNonce) = DeserializeTwoByteArrays(serialized);
        var grade = (int)gradeBytes[0] - 3;
        return (grade, gradeNonce);
    }
}
