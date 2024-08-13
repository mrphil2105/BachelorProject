using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography;
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
    
    [AutoData, Theory]
    public void EqualityProof_Verify_ShouldReturnTrue_WhenProofIsValid(byte[] value)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var proof = EqualityProof.Create(value, _b1, _b2);
        
        var c1 = Commitment.Create(value, _b1);
        var c2 = Commitment.Create(value, _b2);

        var actual = proof.Verify(c1, c2);
        
        actual.Should().BeTrue();

    }
    
    
    /*
    [AutoData, Theory]
    public void EqualityProof_Verify_ShouldReturnFalse_WhenProofIsInvalid(byte[] value)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var hash = SHA512.HashData(value);
        var hashInteger = new BigInteger(hash);
        
        var c1 = parameters.G.Multiply(_b1).Add(Commitment.HPoint.Multiply(hashInteger));
        var c2= parameters.G.Multiply(_b2).Add(Commitment.HPoint.Multiply(hashInteger));

        var proof = EqualityProof.Create(c1, c2, _b1, _b2);
        
        bool actual = proof.Verify(c1, Commitment.HPoint.Multiply(GenerateBigInteger()));
        
        actual.Should().BeFalse();
    }
    */
    
    /*
    [AutoData, Theory]
    public void EqualityProof_ToBytes_ShouldReturnEqualProof(byte[] value)
    {
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var hash = SHA512.HashData(value);
        var hashInteger = new BigInteger(hash);
        
        var c1 = parameters.G.Multiply(_b1).Add(Commitment.HPoint.Multiply(hashInteger));
        var c2= parameters.G.Multiply(_b2).Add(Commitment.HPoint.Multiply(hashInteger));

        var proof = EqualityProof.Create(c1, c2, _b1, _b2);
        
        var bytes = proof.ToBytes();
        var actual = EqualityProof.FromBytes(bytes);
        
        actual.Should().BeEquivalentTo(proof);
    }
    */
}