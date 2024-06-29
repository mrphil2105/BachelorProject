namespace Apachi.ViewModels.Services;

public interface IApiService
{
    Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest requestContent);

    Task<TResponse> GetAsync<TResponse>(string path, IDictionary<string, string> queryParameters);

    Task<Stream> GetFileAsync(string path, IDictionary<string, string> queryParameters);
}
