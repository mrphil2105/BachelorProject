using Apachi.Shared.Crypto;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class NIZKProofTests
{
    private readonly BigInteger _privateKey;
    private readonly ECPoint _g;
    private readonly ECPoint _h;

    public NIZKProofTests()
    {
        _privateKey = DataUtils.GenerateBigInteger();
        var curveParams = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        var parameters = new ECDomainParameters(curveParams.Curve, curveParams.G, curveParams.N, curveParams.H);
        _g = parameters.G;
        _h = parameters.Curve.CreatePoint(parameters.G.XCoord.ToBigInteger(), 
            parameters.G.YCoord.ToBigInteger()).Multiply(new BigInteger("2"));
    }
    
    [Fact]
    public void NIZKProof_Verify_ShouldReturnTrue_WhenProofIsValid()
    {
        var y = _g.Multiply(_privateKey);
        var z = _h.Multiply(_privateKey);
        
        var proof = NIZKProof.Create(_g, _h, _privateKey);
        
        bool actual = proof.Verify(_g, _h, y, z);
        
        actual.Should().BeTrue();
    }
    
    [Fact]
    public void NIZKProof_Verify_ShouldReturnFalse_WhenProofIsInvalid()
    {
        var y = _g.Multiply(_privateKey);
        var z = _h.Multiply(_privateKey);

        var invalidPrivateKey = DataUtils.GenerateBigInteger();
        var proof = NIZKProof.Create(_g, _h, invalidPrivateKey);
        
        bool actual = proof.Verify(_g, _h, y, z);
        
        actual.Should().BeFalse();
    }
    
    [Fact]
    public void NIZKProof_FromBytesToBytes_ShouldReturnTrue_WhenProofIsValid()
    {
        var proof = NIZKProof.Create(_g, _h, _privateKey);
        byte[] serialized = proof.ToBytes();
        
        NIZKProof deserialized = NIZKProof.FromBytes(serialized);
        
        var y = _g.Multiply(_privateKey);
        var z = _h.Multiply(_privateKey);
        bool actual = deserialized.Verify(_g, _h, y, z);
        
        actual.Should().BeTrue();
    }
    
    [Fact]
    public void NIZKProof_FromBytesToBytes_ShouldReturnFalse_WhenProofIsInvalid()
    {
        var proof = NIZKProof.Create(_g, _h, _privateKey);
        byte[] serialized = proof.ToBytes();
        
        NIZKProof deserialized = NIZKProof.FromBytes(serialized);
        
        var y = _g.Multiply(_privateKey);
        var z = _h.Multiply(DataUtils.GenerateBigInteger());
        bool actual = deserialized.Verify(_g, _h, y, z);

        actual.Should().BeFalse();
    }
}