using Apachi.ProgramCommittee.Data;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Services;

public class ShareReviewRandomnessWithReviewersProcessor : IJobProcessor
{
    private readonly LogDbContext _logDbContext;

    public ShareReviewRandomnessWithReviewersProcessor(LogDbContext logDbContext)
    {
        _logDbContext = logDbContext;
    }

    public async Task ProcessJobAsync(Job job, CancellationToken cancellationToken)
    {
        var submissionMessage = await _logDbContext.GetMessageAsync<SubmissionMessage>(job.SubmissionId);

        var pcPrivateKey = KeyUtils.GetPCPrivateKey();
        var submissionKey = await EncryptionUtils.AsymmetricDecryptAsync(
            submissionMessage.EncryptedSubmissionKey,
            pcPrivateKey
        );

        var paperBytes = await EncryptionUtils.SymmetricDecryptAsync(
            submissionMessage.EncryptedPaper,
            submissionKey,
            null
        );
        var reviewRandomness = await EncryptionUtils.SymmetricDecryptAsync(
            submissionMessage.EncryptedReviewRandomness,
            submissionKey,
            null
        );

        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(paperBytes);
        await memoryStream.WriteAsync(reviewRandomness);
        var bytesToSign = memoryStream.ToArray();
        var signature = await KeyUtils.CalculateSignatureAsync(bytesToSign, pcPrivateKey);

        var matchingMessage = await _logDbContext.GetMessageAsync<PaperReviewersMatchingMessage>(job.SubmissionId);
        var reviewerPublicKeys = DataUtils.DeserializeByteArrays(matchingMessage.ReviewerPublicKeys);
        var reviewers = await _logDbContext
            .Reviewers.Where(reviewer => reviewerPublicKeys.Any(key => reviewer.PublicKey.SequenceEqual(key)))
            .ToListAsync();

        foreach (var reviewer in reviewers)
        {
            var sharedKey = await EncryptionUtils.AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
            var encryptedPaper = await EncryptionUtils.SymmetricEncryptAsync(paperBytes, sharedKey, null);
            var encryptedReviewRandomness = await EncryptionUtils.SymmetricEncryptAsync(
                reviewRandomness,
                sharedKey,
                null
            );

            var shareMessage = new ReviewRandomnessReviewerShareMessage(
                encryptedPaper,
                encryptedReviewRandomness,
                signature
            );
            _logDbContext.AddMessage(job.SubmissionId, shareMessage);
        }

        await _logDbContext.SaveChangesAsync();
    }
}
