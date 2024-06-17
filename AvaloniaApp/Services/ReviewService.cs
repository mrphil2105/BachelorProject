using System.Security.Cryptography;
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
    private readonly IApiService _apiService;

    public ReviewService(ISessionService sessionService, Func<AppDbContext> dbContextFactory, IApiService apiService)
    {
        _sessionService = sessionService;
        _dbContextFactory = dbContextFactory;
        _apiService = apiService;
    }

    public async Task<List<OpenSubmissionDto>> GetOpenSubmissionsAsync()
    {
        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = new Dictionary<string, string>() { { "reviewerId", reviewerId.ToString() } };
        var openSubmissionDtos = await _apiService
            .GetAsync<List<OpenSubmissionDto>>("Review/GetOpenSubmissions", queryParameters)
            .ConfigureAwait(false);
        return openSubmissionDtos;
    }

    public async Task DownloadPaperAsync(Guid submissionId, byte[] paperSignature, string paperFilePath)
    {
        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = new Dictionary<string, string>()
        {
            { "submissionId", submissionId.ToString() },
            { "reviewerId", reviewerId.ToString() }
        };

        await using var contentStream = await _apiService
            .GetFileAsync("Review/GetPaper", queryParameters)
            .ConfigureAwait(false);

        await using var dbContext = _dbContextFactory();
        var reviewer = await dbContext.Reviewers.SingleOrDefaultAsync(reviewer => reviewer.Id == reviewerId);
        var sharedKey = await _sessionService.SymmetricDecryptAsync(reviewer!.EncryptedSharedKey);

        var paperBytes = await EncryptionUtils.SymmetricDecryptAsync(contentStream, sharedKey, null);
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();
        var isSignatureValid = await Task.Run(
            () => KeyUtils.VerifySignature(paperBytes, paperSignature, programCommitteePublicKey)
        );

        if (!isSignatureValid)
        {
            throw new CryptographicException("The received paper signature is invalid.");
        }

        await File.WriteAllBytesAsync(paperFilePath, paperBytes);
    }

    public async Task SendBidAsync(Guid submissionId, bool wantsToReview)
    {
        var reviewerId = _sessionService.UserId!.Value;
        var bidDto = new BidDto(submissionId, reviewerId, wantsToReview);
        var resultDto = await _apiService
            .PostAsync<BidDto, ResultDto>("Review/CreateBid", bidDto)
            .ConfigureAwait(false);

        if (!resultDto.Success)
        {
            throw new OperationFailedException($"Failed to create bid: {resultDto.Message}");
        }
    }
}
