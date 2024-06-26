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
        await ThrowIfInvalidSubmissionSignatureAsync(submitDto);

        var programCommitteePrivateKey = KeyUtils.GetProgramCommitteePrivateKey();
        var submissionCommitmentSignature = await KeyUtils.CalculateSignatureAsync(
            submitDto.SubmissionCommitment,
            programCommitteePrivateKey
        );

        var submissionId = Guid.NewGuid();
        var paperBytes = await SavePaperAsync(submitDto, submissionId);
        var paperSignature = await KeyUtils.CalculateSignatureAsync(paperBytes, programCommitteePrivateKey);

        var createdDate = DateTimeOffset.Now;
        var submission = new Submission
        {
            Id = submissionId,
            SubmissionRandomness = submitDto.SubmissionRandomness,
            ReviewRandomness = submitDto.ReviewRandomness,
            SubmissionCommitment = submitDto.SubmissionCommitment,
            IdentityCommitment = submitDto.IdentityCommitment,
            SubmissionPublicKey = submitDto.SubmissionPublicKey,
            SubmissionSignature = submitDto.SubmissionSignature,
            PaperSignature = paperSignature,
            CreatedDate = createdDate,
            UpdatedDate = createdDate
        };
        _dbContext.Submissions.Add(submission);
        await _dbContext.SaveChangesAsync();

        await _jobScheduler.ScheduleJobAsync(JobType.CreateReviews, submissionId.ToString());

        var submittedDto = new SubmittedDto(submissionId, submissionCommitmentSignature);
        _logger.LogInformation("User created new submission with id: {Id}", submissionId);
        return submittedDto;
    }

    private async Task ThrowIfInvalidSubmissionSignatureAsync(SubmitDto submitDto)
    {
        var bytesToVerified = DataUtils.CombineByteArrays(
            submitDto.EncryptedPaper,
            submitDto.EncryptedSubmissionKey,
            submitDto.SubmissionRandomness,
            submitDto.ReviewRandomness,
            submitDto.SubmissionCommitment,
            submitDto.IdentityCommitment
        );
        var isValid = await KeyUtils.VerifySignatureAsync(
            bytesToVerified,
            submitDto.SubmissionSignature,
            submitDto.SubmissionPublicKey
        );

        if (!isValid)
        {
            throw new CryptographicException("The received submission signature is invalid.");
        }
    }

    private async Task<byte[]> SavePaperAsync(SubmitDto submitDto, Guid submissionId)
    {
        var programCommitteePrivateKey = KeyUtils.GetProgramCommitteePrivateKey();
        var submissionKey = await EncryptionUtils.AsymmetricDecryptAsync(
            submitDto.EncryptedSubmissionKey,
            programCommitteePrivateKey
        );

        var submissionsDirectoryPath = _configuration.GetSubmissionsStorage();
        var paperFilePath = Path.Combine(submissionsDirectoryPath, submissionId.ToString());
        Directory.CreateDirectory(submissionsDirectoryPath);

        var paperBytes = await EncryptionUtils.SymmetricDecryptAsync(submitDto.EncryptedPaper, submissionKey, null);
        await System.IO.File.WriteAllBytesAsync(paperFilePath, paperBytes);
        return paperBytes;
    }
}
