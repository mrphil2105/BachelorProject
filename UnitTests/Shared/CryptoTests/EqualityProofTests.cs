using Apachi.Shared.Crypto;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;


namespace Apachi.UnitTests.Shared.CryptoTests;

public class EqualityProofTests
{
    private readonly BigInteger _b1;
    private readonly BigInteger _b2;

    public EqualityProofTests()
    {
        _b1 = GenerateBigInteger();
        _b2 = GenerateBigInteger();
    }
    
    [Fact]
    public void EqualityProof_Verify_ShouldReturnTrue_WhenProofIsValid()
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var v  = GenerateBigInteger();
        
        var c1 = parameters.G.Multiply(_b1).Add(Commitment.HPoint.Multiply(v));
        var c2= parameters.G.Multiply(_b2).Add(Commitment.HPoint.Multiply(v));

        var proof = EqualityProof.Create(c1, c2, _b1, _b2);
        
        bool actual = proof.Verify(c1, c2);
        
        actual.Should().BeTrue();
    }
    
    /*
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
    */
}