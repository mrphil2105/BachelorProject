namespace Apachi.UnitTests.Shared.CryptoTests;

public class EncryptionUtilsTests : IAsyncLifetime
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

    [Theory, AutoData]
    public async Task CryptData_ShouldReturnEncryptedData_WhenCalled(byte[] data)
    {
        byte[] actual = await AsymmetricEncryptAsync(data, _publicKey);
        actual.Should().NotEqual(data);
    }

    [Theory, AutoData]
    public async Task CryptData_ShouldReturnDecryptedData_WhenCalled(byte[] data)
    {
        byte[] encryptedData = await AsymmetricEncryptAsync(data, _publicKey);
        byte[] decryptedData = await AsymmetricDecryptAsync(encryptedData, _privateKey);

        decryptedData.Should().Equal(data);
    }

    [Theory, AutoData]
    public async Task AsymmetricEncrypt_ShouldReturnEncryptedData_WhenCalled(byte[] data)
    {
        byte[] actual = await AsymmetricEncryptAsync(data, _publicKey);
        actual.Should().NotEqual(data);
    }
}
