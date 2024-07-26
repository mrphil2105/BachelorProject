using Apachi.ProgramCommittee.Data;
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

        var pcPrivateKey = GetPCPrivateKey();
        var submissionKey = await AsymmetricDecryptAsync(submissionMessage.EncryptedSubmissionKey, pcPrivateKey);

        var paperBytes = await SymmetricDecryptAsync(submissionMessage.EncryptedPaper, submissionKey);
        var reviewRandomness = await SymmetricDecryptAsync(submissionMessage.EncryptedReviewRandomness, submissionKey);

        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(paperBytes);
        await memoryStream.WriteAsync(reviewRandomness);
        var bytesToSign = memoryStream.ToArray();
        var signature = await CalculateSignatureAsync(bytesToSign, pcPrivateKey);

        var matchingMessage = await _logDbContext.GetMessageAsync<PaperReviewersMatchingMessage>(job.SubmissionId);
        var reviewerPublicKeys = DeserializeByteArrays(matchingMessage.ReviewerPublicKeys);
        var reviewers = await _logDbContext
            .Reviewers.Where(reviewer => reviewerPublicKeys.Any(key => reviewer.PublicKey.SequenceEqual(key)))
            .ToListAsync();

        foreach (var reviewer in reviewers)
        {
            var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
            var encryptedPaper = await SymmetricEncryptAsync(paperBytes, sharedKey);
            var encryptedReviewRandomness = await SymmetricEncryptAsync(reviewRandomness, sharedKey);

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
