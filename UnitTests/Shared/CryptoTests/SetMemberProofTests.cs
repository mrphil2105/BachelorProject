using System.Reflection;
using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Org.BouncyCastle.Math;
using ECPoint = Org.BouncyCastle.Math.EC.ECPoint;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class SetMemberProofTests
{
    private readonly BigInteger _size = new("8");

    public SetMemberProofTests() { }

    [Fact]
    public void Verify_ValidProof_ReturnsTrue()
    {
        var index = BigInteger.Five;

        var proof = SetMemberProof.Create(index, _size);
        var result = proof.Verify(_size);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_InvalidProof_ReturnsFalse()
    {
        var index = _size;

        Action action = () => SetMemberProof.Create(index, _size);

        action
            .Should()
            .ThrowExactly<CryptographicException>()
            .WithMessage(
                $"Unable to create proof for {index.Add(BigInteger.One)} within range 0 to {_size.Add(BigInteger.One)}."
            );
    }

    [Fact]
    public void Verify_EmptySet_ReturnsFalse()
    {
        var index = BigInteger.One;

        var proof = SetMemberProof.Create(index, _size);
        var result = proof.Verify(index);

        result.Should().BeFalse();
    }

    [Fact]
    public void FromBytes_ToBytes_ReturnsExpectedResults()
    {
        var c1Field = typeof(SetMemberProof).GetField("_c1", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var c2Field = typeof(SetMemberProof).GetField("_c2", BindingFlags.Instance | BindingFlags.NonPublic)!;

        var index = BigInteger.Six;
        var proof = SetMemberProof.Create(index, _size);
        var bytes = proof.ToBytes();
        var deserializedProof = SetMemberProof.FromBytes(bytes);

        var c1 = (ECPoint)c1Field.GetValue(proof)!;
        var c2 = (ECPoint)c2Field.GetValue(proof)!;
        var deserializedC1 = (ECPoint)c1Field.GetValue(deserializedProof)!;
        var deserializedC2 = (ECPoint)c2Field.GetValue(deserializedProof)!;

        c1.Should().Be(deserializedC1);
        c2.Should().Be(deserializedC2);
    }
}
