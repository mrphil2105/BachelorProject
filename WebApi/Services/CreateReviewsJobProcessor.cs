using Apachi.Shared.Crypto;
using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Services;

public class CreateReviewsJobProcessor : IJobProcessor
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;

    public CreateReviewsJobProcessor(IConfiguration configuration, AppDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    public async Task<string?> ProcessJobAsync(Job job, CancellationToken stoppingToken)
    {
        var programCommitteePrivateKey = KeyUtils.GetProgramCommitteePrivateKey();
        var submissionId = Guid.Parse(job.Payload!);
        var paperBytes = await GetPaperAsync(submissionId);

        var reviewers = _dbContext.Reviewers.AsAsyncEnumerable();

        await foreach (var reviewer in reviewers)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var alreadyExists = await _dbContext.Reviews.AnyAsync(review =>
                review.SubmissionId == submissionId && review.ReviewerId == reviewer.Id
            );

            if (alreadyExists)
            {
                continue;
            }

            var sharedKey = await EncryptionUtils.AsymmetricDecryptAsync(
                reviewer.EncryptedSharedKey,
                programCommitteePrivateKey
            );
            await SavePaperAsync(submissionId, reviewer.Id, paperBytes, sharedKey);

            var review = new Review { SubmissionId = submissionId, ReviewerId = reviewer.Id };
            _dbContext.Reviews.Add(review);
            await _dbContext.SaveChangesAsync();
        }

        var submission = await _dbContext.Submissions.FirstOrDefaultAsync(submission => submission.Id == submissionId);
        submission!.Status = Shared.SubmissionStatus.Matching;
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

    private async Task SavePaperAsync(Guid submissionId, Guid reviewerId, byte[] paperBytes, byte[] sharedKey)
    {
        var reviewsDirectoryPath = _configuration.GetReviewsStorage();
        var submissionDirectoryPath = Path.Combine(reviewsDirectoryPath, submissionId.ToString());
        var encryptedPaperFilePath = Path.Combine(submissionDirectoryPath, reviewerId.ToString());
        Directory.CreateDirectory(submissionDirectoryPath);

        await using var fileStream = File.Create(encryptedPaperFilePath);
        await EncryptionUtils.SymmetricEncryptAsync(paperBytes, fileStream, sharedKey, null);
    }
}
