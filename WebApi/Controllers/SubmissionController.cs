using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Math;

namespace Apachi.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class SubmissionController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<SubmissionController> _logger;

    public SubmissionController(
        IConfiguration configuration,
        AppDbContext dbContext,
        ILogger<SubmissionController> logger
    )
    {
        _configuration = configuration;
        _dbContext = dbContext;
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

        var reviewRandomness = new BigInteger(submitDto.ReviewRandomness);
        var reviewCommitment = Commitment.Create(paperBytes, reviewRandomness);
        var reviewNonce = RandomNumberGenerator.GetBytes(16);
        var currentDate = DateTimeOffset.Now;
        var submission = new Submission
        {
            Id = submissionId,
            Title = submitDto.Title,
            Description = submitDto.Description,
            SubmissionRandomness = submitDto.SubmissionRandomness,
            ReviewRandomness = submitDto.ReviewRandomness,
            SubmissionCommitment = submitDto.SubmissionCommitment,
            IdentityCommitment = submitDto.IdentityCommitment,
            SubmissionPublicKey = submitDto.SubmissionPublicKey,
            SubmissionSignature = submitDto.SubmissionSignature,
            PaperSignature = paperSignature,
            ReviewCommitment = reviewCommitment.ToBytes(),
            ReviewNonce = reviewNonce,
            CreatedDate = currentDate,
            UpdatedDate = currentDate
        };
        _dbContext.Submissions.Add(submission);
        await _dbContext.SaveChangesAsync();

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
