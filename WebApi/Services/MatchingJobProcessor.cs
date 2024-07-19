using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Services;

public class MatchingJobProcessor : IJobProcessor
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public MatchingJobProcessor(IConfiguration configuration, AppDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<string?> ProcessJobAsync(Job job, CancellationToken stoppingToken)
    {
        var programCommitteePrivateKey = KeyUtils.GetPCPrivateKey();
        var submissionId = Guid.Parse(job.Payload!);
        var paperBytes = await GetPaperAsync(submissionId);

        var submission = await _dbContext.Submissions.FirstOrDefaultAsync(submission => submission.Id == submissionId);
        var reviews = _dbContext
            .Reviews.Include(review => review.Reviewer)
            .Where(review => review.SubmissionId == submissionId && review.Status == ReviewStatus.Pending)
            .AsAsyncEnumerable();

        var memoryStream = new MemoryStream();

        await foreach (var review in reviews)
        {
            var sharedKey = await EncryptionUtils.AsymmetricDecryptAsync(
                review.Reviewer.EncryptedSharedKey,
                programCommitteePrivateKey
            );
            var encryptedReviewRandomness = await EncryptionUtils.SymmetricEncryptAsync(
                submission!.ReviewRandomness,
                sharedKey,
                null
            );
            review.EncryptedReviewRandomness = encryptedReviewRandomness;
            await memoryStream.WriteAsync(review.Reviewer.ReviewerPublicKey);
        }

        await memoryStream.WriteAsync(submission!.ReviewCommitment);
        await memoryStream.WriteAsync(submission.ReviewNonce);

        var bytesToBeSigned = memoryStream.ToArray();
        var matchingSignature = await KeyUtils.CalculateSignatureAsync(bytesToBeSigned, programCommitteePrivateKey);

        submission.MatchingSignature = matchingSignature;
        submission.Status = SubmissionStatus.Reviewing;
        await _dbContext.SaveChangesAsync();
        return null;
    }

    private async Task<byte[]> GetPaperAsync(Guid submissionId)
    {
        var submissionsDirectoryPath = _configuration.GetSubmissionsStorage();
        var paperFilePath = Path.Combine(submissionsDirectoryPath, submissionId.ToString());
        var paperBytes = await File.ReadAllBytesAsync(paperFilePath);
        return paperBytes;
    }
}
