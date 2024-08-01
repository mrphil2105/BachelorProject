using System.Runtime.InteropServices.JavaScript;
using Apachi.Shared.Crypto;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class SetMemberProofTests
{
    private readonly BigInteger _a;
    private readonly BigInteger _b;
    
    
    
    public SetMemberProofTests()
    {
        _a = BigInteger.Three;
        _b = BigInteger.Ten;
    }
    
    [Fact]
    public void Verify_ValidProof_ReturnsTrue()
    {
        var x = BigInteger.Zero;
        
        var parameters = NistNamedCurves.GetByName(Constants.DefaultCurveName);
        
        var proof = SetMemberProof.Create();
        
        var result = proof.Verify(x);
        
        Assert.True(result);
    }
    
    
    
    
}