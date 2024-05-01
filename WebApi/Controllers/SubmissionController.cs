using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Apachi.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Apachi.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class SubmissionController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly JobScheduler _jobScheduler;
    private readonly ILogger<SubmissionController> _logger;

    public SubmissionController(
        IConfiguration configuration,
        AppDbContext dbContext,
        JobScheduler jobScheduler,
        ILogger<SubmissionController> logger
    )
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<SubmittedDto> Create([FromBody] SubmitDto submitDto)
    {
        ThrowIfInvalidSubmissionSignature(submitDto);

        var programCommitteePrivateKey = KeyUtils.GetProgramCommitteePrivateKey();
        var submissionCommitmentSignature = KeyUtils.CalculateSignature(
            submitDto.SubmissionCommitment,
            programCommitteePrivateKey
        );

        var submissionId = Guid.NewGuid();
        await SavePaperAsync(submitDto, submissionId);

        var submission = new Submission
        {
            Id = submissionId,
            SubmissionRandomness = submitDto.SubmissionRandomness,
            ReviewRandomness = submitDto.ReviewRandomness,
            SubmissionCommitment = submitDto.SubmissionCommitment,
            IdentityCommitment = submitDto.IdentityCommitment,
            SubmissionPublicKey = submitDto.SubmissionPublicKey,
            SubmissionSignature = submitDto.SubmissionSignature
        };
        _dbContext.Submissions.Add(submission);
        await _dbContext.SaveChangesAsync();

        await _jobScheduler.ScheduleJobAsync(JobType.CreateReviews, submissionId.ToString());

        var submittedDto = new SubmittedDto(submissionId, submissionCommitmentSignature);
        _logger.LogInformation("User created new submission with id: {Id}", submissionId);
        return submittedDto;
    }

    private void ThrowIfInvalidSubmissionSignature(SubmitDto submitDto)
    {
        var bytesToVerified = DataUtils.CombineByteArrays(
            submitDto.EncryptedPaper,
            submitDto.EncryptedSubmissionKey,
            submitDto.SubmissionRandomness,
            submitDto.ReviewRandomness,
            submitDto.SubmissionCommitment,
            submitDto.IdentityCommitment
        );
        var isValid = KeyUtils.VerifySignature(
            bytesToVerified,
            submitDto.SubmissionSignature,
            submitDto.SubmissionPublicKey
        );

        if (!isValid)
        {
            throw new CryptographicException("The received submission signature is invalid.");
        }
    }

    private async Task SavePaperAsync(SubmitDto submitDto, Guid submissionId)
    {
        var programCommitteePrivateKey = KeyUtils.GetProgramCommitteePrivateKey();
        var submissionKey = EncryptionUtils.AsymmetricDecrypt(
            submitDto.EncryptedSubmissionKey,
            programCommitteePrivateKey
        );
        var paperBytes = await EncryptionUtils.SymmetricDecryptAsync(submitDto.EncryptedPaper, submissionKey, null);
        var submissionsDirectoryPath = _configuration.GetSection("Storage").GetValue<string>("Submissions");

        if (submissionsDirectoryPath == null)
        {
            throw new InvalidOperationException(
                "A Submissions storage directory must be specified in application settings."
            );
        }

        var paperFilePath = Path.Combine(submissionsDirectoryPath, submissionId.ToString());
        Directory.CreateDirectory(submissionsDirectoryPath);
        await System.IO.File.WriteAllBytesAsync(paperFilePath, paperBytes);
    }
}
