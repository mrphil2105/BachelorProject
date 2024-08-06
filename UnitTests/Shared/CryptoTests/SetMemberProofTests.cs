using System.Runtime.InteropServices.JavaScript;
using Apachi.Shared.Crypto;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class SetMemberProofTests
{
    
    private readonly List<(BigInteger g, BigInteger g_r)> _gradesList;
    
    public SetMemberProofTests()
    {
        _gradesList = new List<(BigInteger g, BigInteger g_r)>
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
        
        var proof = SetMemberProof.Create(_gradesList);
        
        var result = proof.Verify(x);
        
        Assert.True(result);
    }
    
    [Fact]
    public void Verify_InvalidProof_ReturnsFalse()
    {
        var x = BigInteger.Five;
        
        var proof = SetMemberProof.Create(_gradesList);
        
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
        var proof = SetMemberProof.Create(_gradesList);
        
        var result1 = proof.Verify(BigInteger.One);
        var result2 = proof.Verify(BigInteger.Two);
        var result3 = proof.Verify(BigInteger.Three);
        
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeFalse();
    }
    
    
    /*
    [Fact]
    public void FromBytes_ToBytes_ReturnsExpectedResults()
    {
        var proof = SetMemberProof.Create(_gradesList);
        var bytes = proof.ToBytes();
        var deserializedProof = SetMemberProof.FromBytes(bytes); ;

        proof.Equals(deserializedProof).Should().BeTrue();
    }
    */
}