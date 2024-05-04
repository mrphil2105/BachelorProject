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

    public async Task<List<SubmissionToReviewDto>> GetSubmissionsToReviewAsync()
    {
        var httpClient = _httpClientFactory.CreateClient();
        using var response = await httpClient.GetAsync("Review/GetSubmissionsToReview");

        var submissionsToReviewJson = await response.Content.ReadAsStringAsync();
        var submissionToReviewDtos = JsonSerializer.Deserialize<List<SubmissionToReviewDto>>(submissionsToReviewJson);
        return submissionToReviewDtos ?? new List<SubmissionToReviewDto>();
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
}
