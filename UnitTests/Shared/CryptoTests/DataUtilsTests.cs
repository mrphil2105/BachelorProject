using Apachi.Shared.Crypto;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class DataUtilsTests
{
    private readonly BigInteger[] _bigIntegers;

    public DataUtilsTests()
    {
        Random random = new Random();
        int randomCount = random.Next(1, 256);
        _bigIntegers = GenerateBigIntegerList(randomCount);
    }

    private BigInteger[] GenerateBigIntegerList(int count)
    {
        BigInteger[] integers = new BigInteger[count];

        for (int i = 0; i < integers.Length; i++)
        {
            integers[i] = DataUtils.GenerateBigInteger();
        }

        return integers;
    }

    [Fact]
    public void SerializeBigIntegers_ShouldReturnSerializedIntegers_WhenCalled()
    {
        byte[] actual = DataUtils.SerializeBigIntegers(_bigIntegers);

        actual.Should().NotBeNull();
    }

    [Fact]
    public void DeserializeBigIntegers_ShouldReturnDeserializedIntegers_WhenCalled()
    {
        byte[] serialized = DataUtils.SerializeBigIntegers(_bigIntegers);
        List<BigInteger> actual = DataUtils.DeserializeBigIntegers(serialized);

        actual.Should().NotBeNull();
    }

    [Fact]
    public void SerializeBigIntegers_ShouldThrowArgumentException_WhenIntegersExceedByteMaxValue()
    {
        BigInteger[] integers = GenerateBigIntegerList(byte.MaxValue + 1);

        Action action = () => DataUtils.SerializeBigIntegers(integers);

        action.Should().Throw<ArgumentException>();
    }
}
