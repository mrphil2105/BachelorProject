using Apachi.Shared.Crypto;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;


namespace Apachi.UnitTests.Shared.CryptoTests;

public class EqualityProofTests
{
    private readonly byte[] _privateKey;

    public EqualityProofTests()
    {
        _privateKey = GenerateBigInteger().ToByteArray();
    }
    
    [Fact]
    public void NIZKProof_Verify_ShouldReturnTrue_WhenProofIsValid()
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var x = new BigInteger(_privateKey);
        
        var y = parameters.G.Multiply(x);
        var z = Commitment.HPoint.Multiply(x);
        
        var proof = EqualityProof.Create(_privateKey);
        
        bool actual = proof.Verify(y, z);
        
        actual.Should().BeTrue();
    }
    
    [Fact]
    public void NIZKProof_Verify_ShouldReturnFalse_WhenProofIsInvalid()
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var x = new BigInteger(_privateKey);
        
        var y = parameters.G.Multiply(x);
        var z = Commitment.HPoint.Multiply(x);

        var invalidPrivateKey = GenerateBigInteger().ToByteArray();
        var proof = EqualityProof.Create(invalidPrivateKey);
        
        bool actual = proof.Verify(y, z);
        
        actual.Should().BeFalse();
    }
    
    [Fact]
    public void NIZKProof_FromBytesToBytes_ShouldReturnTrue_WhenProofIsValid()
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var x = new BigInteger(_privateKey);
        
        var proof = EqualityProof.Create(_privateKey);
        byte[] serialized = proof.ToBytes();
        
        EqualityProof deserialized = EqualityProof.FromBytes(serialized);
        
        var y = parameters.G.Multiply(x);
        var z = Commitment.HPoint.Multiply(x);
        bool actual = deserialized.Verify(y, z);
        
        actual.Should().BeTrue();
    }
    
    [Fact]
    public void NIZKProof_FromBytesToBytes_ShouldReturnFalse_WhenProofIsInvalid()
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var x = new BigInteger(_privateKey);
        
        var proof = EqualityProof.Create(_privateKey);
        byte[] serialized = proof.ToBytes();
        
        EqualityProof deserialized = EqualityProof.FromBytes(serialized);
        
        var y = parameters.G.Multiply(x);
        var z = Commitment.HPoint.Multiply(GenerateBigInteger());
        bool actual = deserialized.Verify(y, z);

        actual.Should().BeFalse();
    }
}