using System.Text;
using System.Text.Json;
using Apachi.ViewModels.Services;

namespace Apachi.AvaloniaApp.Services;

public class ApiService : IApiService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest requestContent)
    {
        var requestJson = JsonSerializer.Serialize(requestContent);
        using var jsonContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var httpClient = _httpClientFactory.CreateClient();

        using var responseMessage = await httpClient.PostAsync(path, jsonContent);
        var responseJson = await responseMessage.Content.ReadAsStringAsync();
        var responseContent = JsonSerializer.Deserialize<TResponse>(responseJson)!;

        if (responseContent == null)
        {
            throw new InvalidDataException("Received a null response from API.");
        }

        return responseContent;
    }

    public async Task<TResponse> GetAsync<TResponse>(string path, IDictionary<string, string> queryParameters)
    {
        var queryString = EncodeQueryParameters(queryParameters);
        var httpClient = _httpClientFactory.CreateClient();

        using var responseMessage = await httpClient.GetAsync(path + queryString);
        var responseJson = await responseMessage.Content.ReadAsStringAsync();
        var responseContent = JsonSerializer.Deserialize<TResponse>(responseJson);

        if (responseContent == null)
        {
            throw new InvalidDataException("Received a null response from API.");
        }

        return responseContent;
    }

    public async Task<Stream> GetFileAsync(string path, IDictionary<string, string> queryParameters)
    {
        var queryString = EncodeQueryParameters(queryParameters);
        var httpClient = _httpClientFactory.CreateClient();

        var responseMessage = await httpClient.GetAsync(path + queryString);
        var contentStream = await responseMessage.Content.ReadAsStreamAsync();
        return contentStream;
    }

    private static string EncodeQueryParameters(IDictionary<string, string> queryParameters)
    {
        var builder = new StringBuilder();

        foreach (var (key, value) in queryParameters)
        {
            builder.Append(builder.Length == 0 ? '?' : '&');
            builder.Append(key);
            builder.Append('=');
            var escapedValue = Uri.EscapeDataString(value);
            builder.Append(escapedValue);
        }

        return builder.ToString();
    }
}
