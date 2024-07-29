using System.Runtime.InteropServices.JavaScript;
using Apachi.Shared.Crypto;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class SetMemberProofTests
{
    private readonly Commitment _commitment;
    private readonly BigInteger _randomness;
    private readonly SetMemberProof _proof;
    
    public SetMemberProofTests()
    {
        var value = BigInteger.Two.ToByteArray();
        _randomness = DataUtils.GenerateBigInteger();
        _commitment = Commitment.Create(value, _randomness);
        var a = BigInteger.One;
        var b = BigInteger.Five;
        _proof = SetMemberProof.Create(a, b);
    }
    
    [Fact]
    public void Verify_ValidProof_ReturnsTrue()
    {
        var result = _proof.Verify(_commitment, _randomness, BigInteger.Two);
        result.Should().BeTrue();
    }
    
    [Fact]
    public void Verify_InvalidProof_ReturnsFalse()
    {
        var result = _proof.Verify(_commitment, _randomness, BigInteger.Ten);
        result.Should().BeFalse();
    }
    
    
}