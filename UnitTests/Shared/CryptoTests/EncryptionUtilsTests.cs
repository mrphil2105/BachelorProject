using System.Security.Cryptography;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class EncryptionUtilsTests : IAsyncLifetime
{
    private readonly byte[] _aesKey;
    private readonly byte[] _hmacKey;

    private byte[] _privateKey = null!;
    private byte[] _publicKey = null!;

    public EncryptionUtilsTests()
    {
        _aesKey = RandomNumberGenerator.GetBytes(32);
        _hmacKey = RandomNumberGenerator.GetBytes(32);
    }

    public async Task InitializeAsync()
    {
        (_privateKey, _publicKey) = await GenerateKeyPairAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Theory, AutoData]
    public async Task SymmetricEncrypt_ShouldReturnEncryptedData([ArraySize(10)] byte[] data)
    {
        var encrypted = await SymmetricEncryptAsync(data, _aesKey);

        var iv = new byte[16];
        Buffer.BlockCopy(encrypted, 0, iv, 0, 16);
        using var aes = Aes.Create();
        var encryptor = aes.CreateEncryptor(_aesKey, iv);
        var expectedCiphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        var ciphertext = encrypted.Skip(16);
        ciphertext.ToArray().Should().Equal(expectedCiphertext);
    }

    [Theory, AutoData]
    public async Task SymmetricDecrypt_ShouldReturnDecryptedData([ArraySize(10)] byte[] data)
    {
        var encrypted = await SymmetricEncryptAsync(data, _aesKey);
        var decrypted = await SymmetricDecryptAsync(encrypted, _aesKey);

        decrypted.Should().Equal(data);
    }

    [Theory, AutoData]
    public async Task SymmetricEncryptAndMac_ShouldReturnEncryptedData([ArraySize(10)] byte[] data)
    {
        var encryptedAndMac = await SymmetricEncryptAndMacAsync(data, _aesKey, _hmacKey);

        var iv = new byte[16];
        Buffer.BlockCopy(encryptedAndMac, 32, iv, 0, 16);
        using var aes = Aes.Create();
        var encryptor = aes.CreateEncryptor(_aesKey, iv);
        var expectedCiphertext = encryptor.TransformFinalBlock(data, 0, data.Length);

        var actualHash = encryptedAndMac.Take(32).ToArray();
        var encrypted = encryptedAndMac.Skip(32).ToArray();
        var expectedHash = new HMACSHA256(_hmacKey).ComputeHash(encrypted);

        var ciphertext = encrypted.Skip(16);
        ciphertext.ToArray().Should().Equal(expectedCiphertext);
        actualHash.Should().Equal(expectedHash);
    }

    [Theory, AutoData]
    public async Task SymmetricDecryptAndVerify_ShouldReturnDecryptedData([ArraySize(10)] byte[] data)
    {
        var encrypted = await SymmetricEncryptAndMacAsync(data, _aesKey, _hmacKey);
        var decrypted = await SymmetricDecryptAndVerifyAsync(encrypted, _aesKey, _hmacKey);

        decrypted.Should().Equal(data);
    }

    [Theory, AutoData]
    public async Task AsymmetricEncrypt_ShouldReturnEncryptedData([ArraySize(10)] byte[] data)
    {
        var actual = await AsymmetricEncryptAsync(data, _publicKey);
        actual.Should().NotEqual(data);
    }

    [Theory, AutoData]
    public async Task AsymmetricDecrypt_ShouldReturnDecryptedData([ArraySize(10)] byte[] data)
    {
        var encrypted = await AsymmetricEncryptAsync(data, _publicKey);
        var decrypted = await AsymmetricDecryptAsync(encrypted, _privateKey);

        decrypted.Should().Equal(data);
    }
}
