using Apachi.Shared.Crypto;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class DataUtilsTests
{
    private readonly BigInteger[] _integers;
    private readonly Random _random;

    public DataUtilsTests()
    {
        _random = new Random();

        // arbitrary number of BigIntegers
        _integers = new BigInteger[10];

        for (int i = 0; i < _integers.Length; i++)
        {
            _integers[i] = GenerateBigInteger();
        }
    }

    private BigInteger GenerateBigInteger()
    {
        byte[] bytes = new byte[32];
        _random.NextBytes(bytes);
        return new BigInteger(bytes);
    }

    [Fact]
    public void SerializeBigIntegers_ShouldReturnSerializedIntegers_WhenCalled()
    {
        byte[] actual = DataUtils.SerializeBigIntegers(_integers);

        actual.Should().NotBeNull();
    }

    [Fact]
    public void DeserializeBigIntegers_ShouldReturnDeserializedIntegers_WhenCalled()
    {
        byte[] serialized = DataUtils.SerializeBigIntegers(_integers);
        List<BigInteger> actual = DataUtils.DeserializeBigIntegers(serialized);

        actual.Should().NotBeNull();
    }

    [Fact]
    public void SerializeBigIntegers_ShouldThrowArgumentException_WhenIntegersExceedByteMaxValue()
    {
        BigInteger[] integers = new BigInteger[256];

        for (int i = 0; i < integers.Length; i++)
        {
            integers[i] = GenerateBigInteger();
        }

        Action action = () => DataUtils.SerializeBigIntegers(integers);

        action.Should().Throw<ArgumentException>();
    }
}
