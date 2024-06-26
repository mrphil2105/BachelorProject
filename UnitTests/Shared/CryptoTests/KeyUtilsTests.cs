using Apachi.Shared.Crypto;
using AutoFixture;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class KeyUtilsTests : IAsyncLifetime
{
    private (byte[] PublicKey, byte[] PrivateKey) _keyPair;

    public async Task InitializeAsync()
    {
        _keyPair = await KeyUtils.GenerateKeyPairAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public void GenerateKeyPair_ShouldReturnKeyPair_WhenCalled()
    {
        var (actualPub, actualPriv) = _keyPair;
        actualPub.Should().NotBeNull();
        actualPriv.Should().NotBeNull();
    }

    [AutoData]
    [Theory]
    public void CalculateSignature_ShouldReturnSignature_WhenCalled(byte[] data)
    {
        var actual = KeyUtils.CalculateSignatureAsync(data, _keyPair.PrivateKey);
        actual.Should().NotBeNull();
    }

    [AutoData]
    [Theory]
    public async void VerifySignature_ShouldReturnTrue_WhenCalled(byte[] data)
    {
        var signature = await KeyUtils.CalculateSignatureAsync(data, _keyPair.PrivateKey);
        var actual = await KeyUtils.VerifySignatureAsync(data, signature, _keyPair.PublicKey);
        
        actual.Should().BeTrue();
    }
}
