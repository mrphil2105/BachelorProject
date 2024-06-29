using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Services;

public static class ApiServiceExtensions
{
    public static async Task<TResponse> PostEncryptedSignedAsync<TRequest, TResponse>(
        this IApiService apiService,
        string path,
        TRequest requestContent,
        Guid identifier,
        byte[] aesKey,
        byte[] privateKey
    )
    {
        var encryptedSignedDto = await EncryptedSignedDto.FromDtoAsync(requestContent, identifier, aesKey, privateKey);
        return await apiService.PostAsync<EncryptedSignedDto, TResponse>(path, encryptedSignedDto);
    }
}
