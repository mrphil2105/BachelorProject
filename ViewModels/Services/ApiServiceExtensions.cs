using Apachi.Shared.Dtos;

namespace Apachi.ViewModels.Services;

public static class ApiServiceExtensions
{
    public static async Task<TResponse> PostEncryptedSignedAsync<TRequest, TResponse>(
        this IApiService apiService,
        string path,
        TRequest requestContent,
        byte[] aesKey,
        byte[] privateKey
    )
    {
        var encryptedSignedDto = await EncryptedSignedDto.FromDtoAsync(requestContent, aesKey, privateKey);
        return await apiService.PostAsync<EncryptedSignedDto, TResponse>(path, encryptedSignedDto);
    }
}
