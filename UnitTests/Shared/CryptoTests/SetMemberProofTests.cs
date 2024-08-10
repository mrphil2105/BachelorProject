using System.Reflection;
using Apachi.Shared.Crypto;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class SetMemberProofTests
{
    private readonly List<(BigInteger g, BigInteger g_r)> _grades;

    public SetMemberProofTests()
    {
        _grades = new List<(BigInteger g, BigInteger g_r)>
        {
            (BigInteger.One, DataUtils.GenerateBigInteger()),
            (BigInteger.Two, DataUtils.GenerateBigInteger()),
            (BigInteger.Three, DataUtils.GenerateBigInteger())
        };
    }

    [Fact]
    public void Verify_ValidProof_ReturnsTrue()
    {
        var x = BigInteger.Two;

        var proof = SetMemberProof.Create(_grades);

        var result = proof.Verify(x);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_InvalidProof_ReturnsFalse()
    {
        var x = BigInteger.Five;

        var proof = SetMemberProof.Create(_grades);

        var result = proof.Verify(x);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_EmptySet_ReturnsFalse()
    {
        var emptyGradesList = new List<(BigInteger g, BigInteger g_r)>();

        var x = BigInteger.One;

        var proof = SetMemberProof.Create(emptyGradesList);

        var result = proof.Verify(x);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_BoundaryValues_ReturnsExpectedResults()
    {
        var proof = SetMemberProof.Create(_grades);

        var result1 = proof.Verify(BigInteger.One);
        var result2 = proof.Verify(BigInteger.Two);
        var result3 = proof.Verify(BigInteger.Three);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeFalse();
    }

    [Fact]
    public void FromBytes_ToBytes_ReturnsExpectedResults()
    {
        var field = typeof(SetMemberProof).GetField("_grades", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var proof = SetMemberProof.Create(_grades);
        var bytes = proof.ToBytes();
        var deserializedProof = SetMemberProof.FromBytes(bytes);

        var proofList = (List<(BigInteger, BigInteger)>)field.GetValue(proof)!;
        var deserializedProofList = (List<(BigInteger, BigInteger)>)field.GetValue(deserializedProof)!;

        proofList.Should().Equal(deserializedProofList);
    }
}

