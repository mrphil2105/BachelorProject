using Apachi.Shared.Crypt;
using AutoFixture;

namespace Apachi.UnitTests.Shared.CryptTests;

public class CommitmentTests
{
    [AutoData]
    [Theory]
    public void Commitment_MatchesValue_ShouldReturnTrue_WhenValueIsCorrect(byte[] value)
    {
        Commitment commitment = Commitment.Create(value);

        bool actual = commitment.MatchesValue(value);

        actual.Should().BeTrue();
    }

    [AutoData]
    [Theory]
    public void Commitment_MatchesValue_ShouldReturnFalse_WhenValueIsIncorrect(byte[] value, byte[] incorrectValue)
    {
        Commitment commitment = Commitment.Create(value);

        bool actual = commitment.MatchesValue(incorrectValue);

        actual.Should().BeFalse();
    }

    [AutoData]
    [Theory]
    public void Commitment_FromBytesToBytes_ShouldReturnTrue_WhenValueIsCorrect(byte[] value)
    {
        Commitment commitment = Commitment.Create(value);
        byte[] serialized = commitment.ToBytes();

        Commitment deserialized = Commitment.FromBytes(serialized);
        bool actual = deserialized.MatchesValue(value);

        actual.Should().BeTrue();
    }

    [AutoData]
    [Theory]
    public void Commitment_FromBytesToBytes_ShouldReturnFalse_WhenValueIsIncorrect(byte[] value, byte[] incorrectValue)
    {
        Commitment commitment = Commitment.Create(value);
        byte[] serialized = commitment.ToBytes();

        Commitment deserialized = Commitment.FromBytes(serialized);
        bool actual = deserialized.MatchesValue(incorrectValue);

        actual.Should().BeFalse();
    }
}
