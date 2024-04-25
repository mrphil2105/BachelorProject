using Apachi.Shared.Crypto;

namespace Apachi.UnitTests.Shared.CryptoTests;

public class EncryptionUtilsTests
{
    private readonly (byte[] publicKey, byte[] privateKey) _keyPair;

    public EncryptionUtilsTests()
    {
        _keyPair = KeyUtils.GenerateKeyPair();
    }

    [AutoData]
    [Theory]
    public void CryptData_ShouldReturnEncryptedData_WhenCalled(byte[] data)
    {
        byte[] actual = EncryptionUtils.AsymmetricEncrypt(data, _keyPair.publicKey);

        actual.Should().NotEqual(data);
    }

    [AutoData]
    [Theory]
    public void CryptData_ShouldReturnDecryptedData_WhenCalled(byte[] data)
    {
        byte[] encryptedData = EncryptionUtils.AsymmetricEncrypt(data, _keyPair.publicKey);
        byte[] decryptedData = EncryptionUtils.AsymmetricDecrypt(encryptedData, _keyPair.privateKey);

        decryptedData.Should().Equal(data);
    }

    [AutoData]
    [Theory]
    public void AsymmetricEncrypt_ShouldReturnEncryptedData_WhenCalled(byte[] data)
    {
        byte[] actual = EncryptionUtils.AsymmetricEncrypt(data, _keyPair.publicKey);

        actual.Should().NotEqual(data);
    }
}
