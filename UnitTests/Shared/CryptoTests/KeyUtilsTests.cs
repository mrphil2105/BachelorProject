using Apachi.Shared.Crypto;
using AutoFixture;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class KeyUtilsTests
{
    private readonly (byte[] PublicKey, byte[] PrivateKey) _keyPair;

    public KeyUtilsTests()
    {
        _keyPair = KeyUtils.GenerateKeyPair();
    }

    [Fact]
    public void GenerateKeyPair_ShouldReturnKeyPair_WhenCalled()
    {
        var (actualPub, actualPriv) = KeyUtils.GenerateKeyPair();
        actualPub.Should().NotBeNull();
        actualPriv.Should().NotBeNull();
    }

    [AutoData]
    [Theory]
    public void CalculateSignature_ShouldReturnSignature_WhenCalled(byte[] data)
    {
        var actual = KeyUtils.CalculateSignature(data, _keyPair.PrivateKey);
        actual.Should().NotBeNull();
    }

    [AutoData]
    [Theory]
    public void VerifySignature_ShouldReturnTrue_WhenCalled(byte[] data)
    {
        var signature = KeyUtils.CalculateSignature(data, _keyPair.PrivateKey);

        bool actual = KeyUtils.VerifySignature(data, signature, _keyPair.PublicKey);
        actual.Should().BeTrue();
    }
}
