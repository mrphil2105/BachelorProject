using Apachi.Shared.Crypt;
using AutoFixture;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;

namespace Apachi.UnitTests.Shared.CryptTests;

public class KeyUtilsTests
{
    private readonly Fixture _fixture;
    private readonly AsymmetricCipherKeyPair _keyPair;
    private readonly string _curveName;
    
    public KeyUtilsTests()
    {
        _curveName = "P-521";
        _fixture = new Fixture();
        _keyPair = KeyUtils.GenerateKeyPair();
    }
    
    [Fact]
    public void GenerateKeyPair_ShouldReturnKeyPair_WhenCalled()
    {
        AsymmetricCipherKeyPair actual = KeyUtils.GenerateKeyPair(_curveName);
        actual.Should().NotBeNull();
    }
    
    [Fact]
    public void CreateSignature_ShouldReturnSignature_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();
        
        (BigInteger point, BigInteger signature) actual = KeyUtils.CreateSignature(data, (ECPrivateKeyParameters)_keyPair.Private);

        actual.point.Should().NotBeNull();
        actual.signature.Should().NotBeNull();
    }
    
    [Fact]
    public void VerifySignature_ShouldReturnTrue_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();
        var signature = KeyUtils.CreateSignature(data, (ECPrivateKeyParameters)_keyPair.Private);
        
        bool actual = KeyUtils.VerifySignature(data, signature, (ECPublicKeyParameters)_keyPair.Public);
        actual.Should().BeTrue();
    }
    
    [Fact]
    public void CryptData_ShouldReturnEncryptedData_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();
        byte[] actual = KeyUtils.AsymmetricEncrypt(data, (ECPrivateKeyParameters)_keyPair.Private, (ECPublicKeyParameters)_keyPair.Public);
        
        actual.Should().NotBeNull();
    }
    
    [Fact]
    public void CryptData_ShouldReturnDecryptedData_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();
        byte[] encryptedData = KeyUtils.AsymmetricEncrypt(data, (ECPrivateKeyParameters)_keyPair.Private, (ECPublicKeyParameters)_keyPair.Public);
        byte[] actual = KeyUtils.AsymmetricDecrypt(encryptedData, (ECPrivateKeyParameters)_keyPair.Private, (ECPublicKeyParameters)_keyPair.Public);
        
        actual.Should().BeEquivalentTo(data);
    }
    
    [Fact]
    public void AsymmetricEncrypt_ShouldReturnEncryptedData_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();
        byte[] actual = KeyUtils.AsymmetricEncrypt(data, (ECPrivateKeyParameters)_keyPair.Private, (ECPublicKeyParameters)_keyPair.Public);
        
        actual.Should().NotBeNull();
    }
}