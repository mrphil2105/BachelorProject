using System.Security.Cryptography;
using System.Text;
using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;
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
        Func<AppDbContext> appDbContextFactory,
        Func<LogDbContext> logDbContextFactory,
        IApiService apiService
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
        _apiService = apiService;
    }

    public async Task<List<MatchableSubmissionModel>> GetMatchableSubmissionsAsync()
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);
        var pcPublicKey = GetPCPublicKey();

        await using var logDbContext = _logDbContextFactory();
        var submissionIds = await logDbContext.Entries.Select(entry => entry.SubmissionId).Distinct().ToListAsync();

        var models = new List<MatchableSubmissionModel>();

        foreach (var submissionId in submissionIds)
        {
            var maxStep = await logDbContext.GetMaxProtocolStepAsync(submissionId);

            // If another reviewer has sent their bid the max entry message will be Bid.
            if (!(maxStep is ProtocolStep.PaperReviewerShare or ProtocolStep.Bid))
            {
                continue;
            }

            var shareEntries = await logDbContext.GetEntriesAsync<PaperReviewerShareMessage>(submissionId);
            var matchingShareEntry = await FindMatchingShareAsync(
                shareEntries,
                sharedKey,
                pcPublicKey,
                message => new[] { message.EncryptedPaper },
                message => message.PaperSignature
            );

            if (matchingShareEntry != null)
            {
                var model = new MatchableSubmissionModel(
                    matchingShareEntry.Id,
                    matchingShareEntry.SubmissionId,
                    matchingShareEntry.CreatedDate
                );
                models.Add(model);
            }
        }

        return models;
    }

    public async Task<List<ReviewableSubmissionModel>> GetReviewableSubmissionsAsync()
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);
        var pcPublicKey = GetPCPublicKey();

        await using var logDbContext = _logDbContextFactory();
        var submissionIds = await logDbContext.Entries.Select(entry => entry.SubmissionId).Distinct().ToListAsync();

        var models = new List<ReviewableSubmissionModel>();

        foreach (var submissionId in submissionIds)
        {
            var maxStep = await logDbContext.GetMaxProtocolStepAsync(submissionId);

            // If another reviewer has sent their review the max entry message will be ReviewCommitmentNonceSignature.
            if (!(maxStep is ProtocolStep.ReviewRandomnessReviewerShare or ProtocolStep.ReviewCommitmentNonceSignature))
            {
                continue;
            }

            var shareEntries = await logDbContext.GetEntriesAsync<ReviewRandomnessReviewerShareMessage>(submissionId);
            var matchingShareEntry = await FindMatchingShareAsync(
                shareEntries,
                sharedKey,
                pcPublicKey,
                message => new[] { message.EncryptedPaper, message.EncryptedReviewRandomness },
                message => message.Signature
            );

            if (matchingShareEntry != null)
            {
                var model = new ReviewableSubmissionModel(
                    matchingShareEntry.Id,
                    matchingShareEntry.SubmissionId,
                    matchingShareEntry.CreatedDate
                );
                models.Add(model);
            }
        }

        return models;
    }

    private async Task<LogEntryResult<TMessage>?> FindMatchingShareAsync<TMessage>(
        List<LogEntryResult<TMessage>> shareEntries,
        byte[] sharedKey,
        byte[] pcPublicKey,
        Func<TMessage, IEnumerable<byte[]>> dataSelector,
        Func<TMessage, byte[]> signatureSelector
    )
        where TMessage : IMessage
    {
        // Decrypt each and check the signature to find out if the message is targeted at the current reviewer.
        foreach (var shareEntry in shareEntries)
        {
            await using var memoryStream = new MemoryStream();

            try
            {
                var data = dataSelector(shareEntry.Message);

                foreach (var encrypted in data)
                {
                    var decrypted = await SymmetricDecryptAsync(encrypted, sharedKey);
                    await memoryStream.WriteAsync(decrypted);
                }
            }
            catch (CryptographicException)
            {
                // Ignore exception about invalid padding as it means the data is not encrypted with sharedKey.
                continue;
            }

            var bytesToVerify = memoryStream.ToArray();
            var signature = signatureSelector(shareEntry.Message);
            var isSignatureValid = await VerifySignatureAsync(bytesToVerify, signature, pcPublicKey);

            if (isSignatureValid)
            {
                return shareEntry;
            }
        }

        return null;
    }

    public async Task DownloadPaperAsync(Guid logEntryId, string paperFilePath)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);
        var pcPublicKey = GetPCPublicKey();

        await using var logDbContext = _logDbContextFactory();
        var shareEntry = await logDbContext.GetEntryAsync<PaperReviewerShareMessage>(logEntryId);
        var paperBytes = await SymmetricDecryptAsync(shareEntry.Message.EncryptedPaper, sharedKey);

        await ThrowOnInvalidSignatureAsync(paperBytes, shareEntry.Message.PaperSignature, pcPublicKey);

        await File.WriteAllBytesAsync(paperFilePath, paperBytes);
    }

    public async Task SendBidAsync(Guid logEntryId, bool wantsToReview)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);
        var pcPublicKey = GetPCPublicKey();

        await using var logDbContext = _logDbContextFactory();
        var shareEntry = await logDbContext.GetEntryAsync<PaperReviewerShareMessage>(logEntryId);
        var paperBytes = await SymmetricDecryptAsync(shareEntry.Message.EncryptedPaper, sharedKey);

        await ThrowOnInvalidSignatureAsync(paperBytes, shareEntry.Message.PaperSignature, pcPublicKey);

        var bidBytes = new byte[] { (byte)(wantsToReview ? 1 : 0) };
        var encryptedPaper = await SymmetricEncryptAsync(paperBytes, sharedKey);
        var encryptedBid = await SymmetricEncryptAsync(bidBytes, sharedKey);

        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(paperBytes);
        await memoryStream.WriteAsync(bidBytes);
        var bytesToSign = memoryStream.ToArray();
        var signature = await CalculateSignatureAsync(bytesToSign, privateKey);

        var bidMessage = new BidMessage(encryptedPaper, encryptedBid, signature);
        logDbContext.AddMessage(shareEntry.SubmissionId, bidMessage);
        await logDbContext.SaveChangesAsync();
    }

    public async Task SendReviewAsync(Guid logEntryId, string review)
    {
        await using var appDbContext = _appDbContextFactory();
        var reviewer = await appDbContext.Reviewers.FirstAsync(reviewer =>
            reviewer.Id == _sessionService.UserId!.Value
        );

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedPrivateKey);
        var sharedKey = await _sessionService.SymmetricDecryptAndVerifyAsync(reviewer.EncryptedSharedKey);

        await using var logDbContext = _logDbContextFactory();
        var shareEntry = await logDbContext.GetEntryAsync<ReviewRandomnessReviewerShareMessage>(logEntryId);
        var matchingMessage = await logDbContext.GetMessageAsync<PaperReviewersMatchingMessage>(
            shareEntry.SubmissionId
        );

        var paperBytes = await SymmetricDecryptAsync(shareEntry.Message.EncryptedPaper, sharedKey);

        var reviewBytes = Encoding.UTF8.GetBytes(review);
        var encryptedReview = await SymmetricEncryptAsync(reviewBytes, sharedKey);
        var reviewSignature = await CalculateSignatureAsync(reviewBytes, privateKey);
        var reviewMessage = new ReviewMessage(encryptedReview, reviewSignature);

        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(matchingMessage.ReviewCommitment);
        await memoryStream.WriteAsync(matchingMessage.ReviewNonce);
        var bytesToSign = memoryStream.ToArray();

        var signature = await CalculateSignatureAsync(bytesToSign, privateKey);
        var signatureMessage = new ReviewCommitmentNonceSignatureMessage(signature);

        logDbContext.AddMessage(shareEntry.SubmissionId, reviewMessage);
        logDbContext.AddMessage(shareEntry.SubmissionId, signatureMessage);
        await logDbContext.SaveChangesAsync();
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
        var encryptedAssessment = await _sessionService.SymmetricEncryptAndMacAsync(assessmentBytes);

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

        var assessmentBytes = await _sessionService.SymmetricDecryptAndVerifyAsync(review.EncryptedSavedAssessment);
        var assessment = Encoding.UTF8.GetString(assessmentBytes);
        return assessment;
    }
}
