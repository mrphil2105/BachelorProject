namespace Apachi.UnitTests.Shared.CryptoTests;

public class KeyUtilsTests : IAsyncLifetime
{
    private byte[] _privateKey = null!;
    private byte[] _publicKey = null!;

    public async Task InitializeAsync()
    {
        (_privateKey, _publicKey) = await GenerateKeyPairAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public void GenerateKeyPair_ShouldReturnKeyPair_WhenCalled()
    {
        _publicKey.Should().NotBeNull();
        _privateKey.Should().NotBeNull();
    }

    [AutoData]
    [Theory]
    public void CalculateSignature_ShouldReturnSignature_WhenCalled(byte[] data)
    {
        var actual = CalculateSignatureAsync(data, _privateKey);
        actual.Should().NotBeNull();
    }

    [AutoData]
    [Theory]
    public async void VerifySignature_ShouldReturnTrue_WhenCalled(byte[] data)
    {
        var signature = await CalculateSignatureAsync(data, _privateKey);
        var actual = await VerifySignatureAsync(data, signature, _publicKey);

        actual.Should().BeTrue();
    }
}
