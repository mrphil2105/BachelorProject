using System.Security.Cryptography;
using System.Text;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Services;

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

    public async Task<List<MatchableSubmissionDto>> GetMatchableSubmissionsAsync()
    {
        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = new Dictionary<string, string>() { { "reviewerId", reviewerId.ToString() } };
        var matchableSubmissionDtos = await _apiService.GetAsync<List<MatchableSubmissionDto>>(
            "Review/GetMatchableSubmissions",
            queryParameters
        );
        return matchableSubmissionDtos;
    }

    public async Task DownloadPaperAsync(Guid submissionId, byte[] paperSignature, string paperFilePath)
    {
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();

        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = new Dictionary<string, string>()
        {
            { "submissionId", submissionId.ToString() },
            { "reviewerId", reviewerId.ToString() }
        };

        await using var contentStream = await _apiService.GetFileAsync("Review/GetPaper", queryParameters);

        await using var dbContext = _dbContextFactory();
        var reviewer = await dbContext.Reviewers.FirstOrDefaultAsync(reviewer => reviewer.Id == reviewerId);
        var sharedKey = await _sessionService.SymmetricDecryptAsync(reviewer!.EncryptedSharedKey);

        var paperBytes = await EncryptionUtils.SymmetricDecryptAsync(contentStream, sharedKey, null);
        var isSignatureValid = await KeyUtils.VerifySignatureAsync(
            paperBytes,
            paperSignature,
            programCommitteePublicKey
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
        await using var dbContext = _dbContextFactory();
        var reviewer = await dbContext.Reviewers.FirstOrDefaultAsync(reviewer => reviewer.Id == reviewerId);

        var privateKey = await _sessionService.SymmetricDecryptAsync(reviewer!.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAsync(reviewer.EncryptedSharedKey);

        var bidDto = new BidDto(submissionId, wantsToReview);
        var resultDto = await _apiService.PostEncryptedSignedAsync<BidDto, ResultDto>(
            "Review/CreateBid",
            bidDto,
            reviewerId,
            sharedKey,
            privateKey
        );

        if (!resultDto.Success)
        {
            throw new OperationFailedException($"Failed to create bid: {resultDto.Message}");
        }

        if (wantsToReview)
        {
            var review = new Review { ReviewerId = reviewerId, SubmissionId = submissionId };
            dbContext.Reviews.Add(review);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task SendAssessmentAsync(ReviewableSubmissionDto reviewableSubmissionDto, string assessment)
    {
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();

        var reviewerId = _sessionService.UserId!.Value;
        await using var dbContext = _dbContextFactory();
        var reviewer = await dbContext.Reviewers.FirstOrDefaultAsync(reviewer => reviewer.Id == reviewerId);

        var privateKey = await _sessionService.SymmetricDecryptAsync(reviewer!.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAsync(reviewer.EncryptedSharedKey);

        var reviewRandomness = await EncryptionUtils.SymmetricDecryptAsync(
            reviewableSubmissionDto.EncryptedReviewRandomness,
            sharedKey,
            null
        );
        var isSignatureValid = await KeyUtils.VerifySignatureAsync(
            reviewRandomness,
            reviewableSubmissionDto.ReviewRandomnessSignature,
            programCommitteePublicKey
        );

        if (!isSignatureValid)
        {
            throw new CryptographicException("The received review randomness signature is invalid.");
        }

        var assessmentBytes = Encoding.UTF8.GetBytes(assessment);
        var encryptedAssessment = await EncryptionUtils.SymmetricEncryptAsync(assessmentBytes, sharedKey, null);
        var assessmentSignature = await KeyUtils.CalculateSignatureAsync(assessmentBytes, privateKey);

        var reviewCommitmentSignature = await KeyUtils.CalculateSignatureAsync(
            reviewableSubmissionDto.ReviewCommitment,
            privateKey
        );
        var reviewNonceSignature = await KeyUtils.CalculateSignatureAsync(
            reviewableSubmissionDto.ReviewNonce,
            privateKey
        );

        var assessmentDto = new AssessmentDto(
            reviewerId,
            reviewableSubmissionDto.SubmissionId,
            encryptedAssessment,
            assessmentSignature,
            reviewCommitmentSignature,
            reviewNonceSignature
        );
        var resultDto = await _apiService.PostAsync<AssessmentDto, ResultDto>("Review/CreateAssessment", assessmentDto);

        if (!resultDto.Success)
        {
            throw new OperationFailedException($"Failed to create assessment: {resultDto.Message}");
        }
    }

    public async Task SaveAssessmentAsync(Guid submissionId, string assessment)
    {
        var reviewerId = _sessionService.UserId!.Value;
        await using var dbContext = _dbContextFactory();
        var review = await dbContext.Reviews.FirstOrDefaultAsync(review =>
            review.ReviewerId == reviewerId && review.SubmissionId == submissionId
        );

        if (review == null)
        {
            throw new OperationFailedException("The review was not found.");
        }

        var assessmentBytes = Encoding.UTF8.GetBytes(assessment);
        var encryptedAssessment = await _sessionService.SymmetricEncryptAsync(assessmentBytes);

        review.EncryptedSavedAssessment = encryptedAssessment;
        await dbContext.SaveChangesAsync();
    }

    public async Task<string?> LoadAssessmentAsync(Guid submissionId)
    {
        var reviewerId = _sessionService.UserId!.Value;
        await using var dbContext = _dbContextFactory();
        var review = await dbContext.Reviews.FirstOrDefaultAsync(review =>
            review.ReviewerId == reviewerId && review.SubmissionId == submissionId
        );

        if (review == null)
        {
            throw new OperationFailedException("The review was not found.");
        }

        if (review.EncryptedSavedAssessment == null)
        {
            return null;
        }

        var assessmentBytes = await _sessionService.SymmetricDecryptAsync(review.EncryptedSavedAssessment);
        var assessment = Encoding.UTF8.GetString(assessmentBytes);
        return assessment;
    }
}
