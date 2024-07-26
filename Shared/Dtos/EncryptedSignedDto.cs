using System.Security.Cryptography;
using System.Text.Json;

namespace Apachi.Shared.Dtos;

public record EncryptedSignedDto(byte[] EncryptedData, byte[] Signature, Guid Identifier)
{
    public static async Task<EncryptedSignedDto> FromDtoAsync<TDto>(
        TDto dto,
        Guid identifier,
        byte[] aesKey,
        byte[] privateKey
    )
    {
        var dtoBytes = JsonSerializer.SerializeToUtf8Bytes(dto);
        var encryptedData = await SymmetricEncryptAsync(dtoBytes, aesKey, null);
        var signature = await CalculateSignatureAsync(dtoBytes, privateKey);
        return new EncryptedSignedDto(encryptedData, signature, identifier);
    }

    public async Task<TDto> ToDtoAsync<TDto>(byte[] aesKey, byte[] publicKey)
    {
        var dtoBytes = await SymmetricDecryptAsync(EncryptedData, aesKey, null);
        var isSignatureValid = await VerifySignatureAsync(dtoBytes, Signature, publicKey);

        if (!isSignatureValid)
        {
            throw new CryptographicException("The received signature is invalid.");
        }

        var dto = JsonSerializer.Deserialize<TDto>(dtoBytes)!;
        return dto;
    }
}
