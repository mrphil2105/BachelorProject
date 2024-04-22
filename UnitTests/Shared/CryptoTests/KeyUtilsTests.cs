using Apachi.Shared.Crypto;
using AutoFixture;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class KeyUtilsTests
{
    private const string CurveName = "P-521";

    private readonly Fixture _fixture;
    private readonly AsymmetricCipherKeyPair _keyPair;

    public KeyUtilsTests()
    {
        _fixture = new Fixture();
        _keyPair = KeyUtils.GenerateKeyPair();
    }

    [Fact]
    public void GenerateKeyPair_ShouldReturnKeyPair_WhenCalled()
    {
        AsymmetricCipherKeyPair actual = KeyUtils.GenerateKeyPair(CurveName);
        actual.Should().NotBeNull();
    }

    [Fact]
    public void CreateSignature_ShouldReturnSignature_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();

        var actual = KeyUtils.CreateSignature(data, (ECPrivateKeyParameters)_keyPair.Private);
        var integers = DataUtils.DeserializeBigIntegers(actual);

        integers[0].Should().NotBeNull();
        integers[1].Should().NotBeNull();
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
        byte[] actual = KeyUtils.AsymmetricEncrypt(
            data,
            (ECPrivateKeyParameters)_keyPair.Private,
            (ECPublicKeyParameters)_keyPair.Public
        );

        actual.Should().NotBeNull();
    }

    [Fact]
    public void CryptData_ShouldReturnDecryptedData_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();
        byte[] encryptedData = KeyUtils.AsymmetricEncrypt(
            data,
            (ECPrivateKeyParameters)_keyPair.Private,
            (ECPublicKeyParameters)_keyPair.Public
        );
        byte[] actual = KeyUtils.AsymmetricDecrypt(
            encryptedData,
            (ECPrivateKeyParameters)_keyPair.Private,
            (ECPublicKeyParameters)_keyPair.Public
        );

        actual.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void AsymmetricEncrypt_ShouldReturnEncryptedData_WhenCalled()
    {
        var data = _fixture.Create<byte[]>();
        byte[] actual = KeyUtils.AsymmetricEncrypt(
            data,
            (ECPrivateKeyParameters)_keyPair.Private,
            (ECPublicKeyParameters)_keyPair.Public
        );

        actual.Should().NotBeNull();
    }
}
