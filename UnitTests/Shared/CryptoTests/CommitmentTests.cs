using Apachi.Shared.Crypto;
using AutoFixture;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class CommitmentTests
{
    private readonly X9ECParameters _ec;
    private readonly BigInteger _randomness;

    public CommitmentTests()
    {
        _ec = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        _randomness = new BigInteger(_ec.N.BitLength, new Random());
    }

    [AutoData]
    [Theory]
    public void Commitment_MatchesValue_ShouldReturnTrue_WhenValueIsCorrect(byte[] value)
    {
        Commitment commitment = Commitment.Create(value, _randomness);

        bool actual = commitment.MatchesValue(value, _randomness);

        actual.Should().BeTrue();
    }

    [AutoData]
    [Theory]
    public void Commitment_MatchesValue_ShouldReturnFalse_WhenValueIsIncorrect(byte[] value, byte[] incorrectValue)
    {
        Commitment commitment = Commitment.Create(value, _randomness);

        bool actual = commitment.MatchesValue(incorrectValue, _randomness);

        actual.Should().BeFalse();
    }

    [AutoData]
    [Theory]
    public void Commitment_FromBytesToBytes_ShouldReturnTrue_WhenValueIsCorrect(byte[] value)
    {
        Commitment commitment = Commitment.Create(value, _randomness);
        byte[] serialized = commitment.ToBytes();

        Commitment deserialized = Commitment.FromBytes(serialized);
        bool actual = deserialized.MatchesValue(value, _randomness);

        actual.Should().BeTrue();
    }

    [AutoData]
    [Theory]
    public void Commitment_FromBytesToBytes_ShouldReturnFalse_WhenValueIsIncorrect(byte[] value, byte[] incorrectValue)
    {
        Commitment commitment = Commitment.Create(value, _randomness);
        byte[] serialized = commitment.ToBytes();

        Commitment deserialized = Commitment.FromBytes(serialized);
        bool actual = deserialized.MatchesValue(incorrectValue, _randomness);

        actual.Should().BeFalse();
    }
}
