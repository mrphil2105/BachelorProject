namespace Apachi.UnitTests.Shared.CryptoTests;

public class EncryptionUtilsTests : IAsyncLifetime
{
    private (byte[] publicKey, byte[] privateKey) _keyPair;

    public async Task InitializeAsync()
    {
        _keyPair = await GenerateKeyPairAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory, AutoData]
    public async Task CryptData_ShouldReturnEncryptedData_WhenCalled(byte[] data)
    {
        byte[] actual = await AsymmetricEncryptAsync(data, _keyPair.publicKey);
        actual.Should().NotEqual(data);
    }

    [Theory, AutoData]
    public async Task CryptData_ShouldReturnDecryptedData_WhenCalled(byte[] data)
    {
        byte[] encryptedData = await AsymmetricEncryptAsync(data, _keyPair.publicKey);
        byte[] decryptedData = await AsymmetricDecryptAsync(encryptedData, _keyPair.privateKey);

        decryptedData.Should().Equal(data);
    }

    [Theory, AutoData]
    public async Task AsymmetricEncrypt_ShouldReturnEncryptedData_WhenCalled(byte[] data)
    {
        byte[] actual = await AsymmetricEncryptAsync(data, _keyPair.publicKey);

        actual.Should().NotEqual(data);
    }
}
