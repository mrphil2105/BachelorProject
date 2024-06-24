using System.Security.Cryptography;
using System.Text.Json;
using Apachi.Shared.Crypto;

namespace Apachi.Shared.Dtos;

public record EncryptedSignedDto(byte[] EncryptedData, byte[] Signature)
{
    public static async Task<EncryptedSignedDto> FromDtoAsync<TDto>(TDto dto, byte[] aesKey, byte[] privateKey)
    {
        var dtoBytes = JsonSerializer.SerializeToUtf8Bytes(dto);
        var encryptedData = await EncryptionUtils.SymmetricEncryptAsync(dtoBytes, aesKey, null);
        var signature = await KeyUtils.CalculateSignatureAsync(dtoBytes, privateKey);
        return new EncryptedSignedDto(encryptedData, signature);
    }

    public async Task<TDto?> ToDtoAsync<TDto>(byte[] aesKey, byte[] publicKey)
    {
        var dtoBytes = await EncryptionUtils.SymmetricDecryptAsync(EncryptedData, aesKey, null);
        var isSignatureValid = await KeyUtils.VerifySignatureAsync(dtoBytes, Signature, publicKey);

        if (!isSignatureValid)
        {
            throw new CryptographicException("The received signature is invalid.");
        }

        var dto = JsonSerializer.Deserialize<TDto>(dtoBytes);
        return dto;
    }
}
