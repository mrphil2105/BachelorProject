using System.Security.Cryptography;
using System.Text;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;
using Apachi.Shared.Dtos;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Services;

public class ReviewService : IReviewService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;
    private readonly IApiService _apiService;

    public ReviewService(
        ISessionService sessionService,
        Func<AppDbContext> dbContextFactory,
        Func<LogDbContext> logDbContextFactory,
        IApiService apiService
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = dbContextFactory;
        _logDbContextFactory = logDbContextFactory;
        _apiService = apiService;
    }

    public async Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync()
    {
        await using var dbContext = _appDbContextFactory();
        var reviewer = await dbContext.Reviewers.FirstOrDefaultAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );
        var sharedKey = await _sessionService.SymmetricDecryptAsync(reviewer!.EncryptedSharedKey);
        var pcPublicKey = KeyUtils.GetPCPublicKey();

        await using var logDbContext = _logDbContextFactory();
        var submissionIds = await logDbContext.Entries.Select(entry => entry.SubmissionId).Distinct().ToListAsync();

        var models = new List<MatchableSubmissionModel>();

        foreach (var submissionId in submissionIds)
        {
            var hasMaxEntry = await logDbContext.HasMaxEntryAsync(submissionId, ProtocolStep.PaperReviewerShare);

            if (!hasMaxEntry)
            {
                continue;
            }

            var entries = await logDbContext.GetEntriesAsync<PaperReviewerShareMessage>(submissionId);
            var matchingEntry = await FindMatchingPaperShareAsync(entries, sharedKey, pcPublicKey);

            if (matchingEntry != null)
            {
                var model = new MatchableSubmissionModel(
                    matchingEntry.Id,
                    matchingEntry.SubmissionId,
                    matchingEntry.CreatedDate
                );
                models.Add(model);
            }
        }

        return models;
    }

    private async Task<LogEntryResult<PaperReviewerShareMessage>?> FindMatchingPaperShareAsync(
        List<LogEntryResult<PaperReviewerShareMessage>> entries,
        byte[] sharedKey,
        byte[] pcPublicKey
    )
    {
        // Decrypt each and check the signature to find out if the message is targeted at the current reviewer.
        foreach (var entry in entries)
        {
            byte[] paperBytes;

            try
            {
                paperBytes = await EncryptionUtils.SymmetricDecryptAsync(entry.Message.EncryptedPaper, sharedKey, null);
            }
            catch (CryptographicException)
            {
                // Ignore exception about invalid padding as it means the paper is not encrypted with sharedKey.
                continue;
            }

            var isSignatureValid = await KeyUtils.VerifySignatureAsync(
                paperBytes,
                entry.Message.PaperSignature,
                pcPublicKey
            );

            if (isSignatureValid)
            {
                return entry;
            }
        }

        return null;
    }

    public async Task DownloadPaperAsync(Guid submissionId, byte[] paperSignature, string paperFilePath)
    {
        var programCommitteePublicKey = KeyUtils.GetPCPublicKey();

        var reviewerId = _sessionService.UserId!.Value;
        var queryParameters = new Dictionary<string, string>()
        {
            { "submissionId", submissionId.ToString() },
            { "reviewerId", reviewerId.ToString() }
        };

        await using var contentStream = await _apiService.GetFileAsync("Review/GetPaper", queryParameters);

        await using var dbContext = _appDbContextFactory();
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
        await using var dbContext = _appDbContextFactory();
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
        var programCommitteePublicKey = KeyUtils.GetPCPublicKey();

        var reviewerId = _sessionService.UserId!.Value;
        await using var dbContext = _appDbContextFactory();
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
        await using var dbContext = _appDbContextFactory();
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
        await using var dbContext = _appDbContextFactory();
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
