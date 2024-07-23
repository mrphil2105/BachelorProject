using Apachi.ProgramCommittee.Data;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Services;

public class SharePaperWithReviewersProcessor : IJobProcessor
{
    private readonly LogDbContext _logDbContext;

    public SharePaperWithReviewersProcessor(LogDbContext logDbContext)
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
        var reviewers = await _logDbContext.Reviewers.ToListAsync();

        foreach (var reviewer in reviewers)
        {
            var sharedKey = await EncryptionUtils.AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
            var encryptedPaper = await EncryptionUtils.SymmetricEncryptAsync(paperBytes, sharedKey, null);
            var paperSignature = await KeyUtils.CalculateSignatureAsync(paperBytes, pcPrivateKey);

            var shareMessage = new PaperReviewerShareMessage(encryptedPaper, paperSignature);
            _logDbContext.AddMessage(job.SubmissionId, shareMessage);
        }

        await _logDbContext.SaveChangesAsync();
    }
}