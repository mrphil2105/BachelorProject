using System.Text;
using System.Text.Json;
using Apachi.AvaloniaApp.Data;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.AvaloniaApp.Services;

public class ReviewService : IReviewService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public ReviewService(
        ISessionService sessionService,
        Func<AppDbContext> dbContextFactory,
        IHttpClientFactory httpClientFactory
    )
    {
        _sessionService = sessionService;
        _dbContextFactory = dbContextFactory;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<OpenSubmissionDto>> GetOpenSubmissionsAsync()
    {
        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = $"?reviewerId={reviewerId}";

        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync($"Review/GetOpenSubmissions{queryParameters}");

        var openSubmissionsJson = await response.Content.ReadAsStringAsync();
        var openSubmissionDtos = JsonSerializer.Deserialize<List<OpenSubmissionDto>>(openSubmissionsJson);
        return openSubmissionDtos ?? new List<OpenSubmissionDto>();
    }

    public async Task DownloadPaperAsync(Guid submissionId, string paperFilePath)
    {
        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = $"?submissionId={submissionId}&reviewerId={reviewerId}";

        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync($"Review/GetPaper{queryParameters}");
        await using var contentStream = await response.Content.ReadAsStreamAsync();

        await using var dbContext = _dbContextFactory();
        var reviewer = await dbContext.Reviewers.SingleOrDefaultAsync(reviewer => reviewer.Id == reviewerId);
        var sharedKey = await _sessionService.SymmetricDecryptAsync(reviewer!.EncryptedSharedKey);

        await using var fileStream = File.Create(paperFilePath);
        await EncryptionUtils.SymmetricDecryptAsync(contentStream, fileStream, sharedKey, null);
    }

    public async Task SendBidAsync(Guid submissionId, bool wantsToReview)
    {
        var reviewerId = _sessionService.UserId!.Value;
        var bidDto = new BidDto(submissionId, reviewerId, wantsToReview);
        var bidJson = JsonSerializer.Serialize(bidDto);
        var jsonContent = new StringContent(bidJson, Encoding.UTF8, "application/json");
        var httpClient = _httpClientFactory.CreateClient();

        using var response = await httpClient.PostAsync("Review/CreateBid", jsonContent);
        response.EnsureSuccessStatusCode();
    }
}
