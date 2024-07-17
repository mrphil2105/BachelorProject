using System.Security.Cryptography;
using System.Text;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Services;

public class ShareAssessmentsJobProcessor : IJobProcessor
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly byte[] _programCommitteePrivateKey;

    public ShareAssessmentsJobProcessor(IConfiguration configuration, AppDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _programCommitteePrivateKey = KeyUtils.GetProgramCommitteePrivateKey();
    }

    public async Task<string?> ProcessJobAsync(Job job, CancellationToken stoppingToken)
    {
        var submissionId = Guid.Parse(job.Payload!);

        var groupKey = RandomNumberGenerator.GetBytes(32);
        var gradeRandomness = DataUtils.GenerateBigInteger();
        var gradeRandomnessBytes = gradeRandomness.ToByteArray();

        var submission = await _dbContext.Submissions.FirstOrDefaultAsync(submission => submission.Id == submissionId);
        submission!.GroupKey = groupKey;
        submission!.GradeRandomness = gradeRandomnessBytes;

        var reviews = await _dbContext
            .Reviews.Include(review => review.Reviewer)
            .Where(review => review.SubmissionId == submissionId && review.Status == ReviewStatus.Discussing)
            .ToListAsync();

        var tasks = reviews
            .Select(review => ShareGroupKeyAndGradeRandomnessAsync(review, groupKey, gradeRandomnessBytes))
            .ToList();
        await Task.WhenAll(tasks);

        var assessmentsSet = new List<byte[]>();

        foreach (var review in reviews)
        {
            var assessmentBytes = Encoding.UTF8.GetBytes(review.Assessment!);
            assessmentsSet.Add(assessmentBytes);
            assessmentsSet.Add(review.AssessmentSignature!);
        }

        var serializedAssessmentsSet = DataUtils.SerializeByteArrays(assessmentsSet);
        submission.EncryptedAssessmentsSet = await EncryptionUtils.SymmetricEncryptAsync(
            serializedAssessmentsSet,
            groupKey,
            null
        );
        submission.AssessmentsSetSignature = await KeyUtils.CalculateSignatureAsync(
            serializedAssessmentsSet,
            _programCommitteePrivateKey
        );

        await _dbContext.SaveChangesAsync();
        return null;
    }

    private async Task ShareGroupKeyAndGradeRandomnessAsync(Review review, byte[] groupKey, byte[] gradeRandomness)
    {
        var sharedKey = await EncryptionUtils.AsymmetricDecryptAsync(
            review.Reviewer.EncryptedSharedKey,
            _programCommitteePrivateKey
        );

        review.EncryptedGroupKey = await EncryptionUtils.SymmetricEncryptAsync(groupKey, sharedKey, null);
        review.GroupKeySignature = await KeyUtils.CalculateSignatureAsync(groupKey, _programCommitteePrivateKey);

        review.EncryptedGradeRandomness = await EncryptionUtils.SymmetricEncryptAsync(gradeRandomness, sharedKey, null);
        review.GradeRandomnessSignature = await KeyUtils.CalculateSignatureAsync(
            gradeRandomness,
            _programCommitteePrivateKey
        );
    }
}
