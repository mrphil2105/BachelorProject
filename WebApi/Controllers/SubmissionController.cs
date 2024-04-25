using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Microsoft.AspNetCore.Mvc;

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
        var programCommitteePrivateKey = GetProgramCommitteePrivateKey();
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

        var submittedDto = new SubmittedDto(submissionId, submissionCommitmentSignature);
        return submittedDto;
    }

    private async Task SavePaperAsync(SubmitDto submitDto, Guid submissionId)
    {
        var programCommitteePrivateKey = GetProgramCommitteePrivateKey();
        var submissionKey = await Task.Run(
            () => EncryptionUtils.AsymmetricDecrypt(submitDto.EncryptedSubmissionKey, programCommitteePrivateKey)
        );
        var paperBytes = await EncryptionUtils.SymmetricDecryptAsync(submitDto.EncryptedPaper, submissionKey);
        var papersDirectory = _configuration.GetSection("Storage").GetValue<string>("Paper");

        if (papersDirectory == null)
        {
            throw new InvalidOperationException("A paper storage directory must be specified in application settings.");
        }

        var paperFilePath = Path.Combine(papersDirectory, submissionId.ToString());
        Directory.CreateDirectory(papersDirectory);
        await System.IO.File.WriteAllBytesAsync(paperFilePath, paperBytes);
    }

    private byte[] GetProgramCommitteePrivateKey()
    {
        var privateKeyBase64 = _configuration.GetValue<string>("APACHI_PC_PRIVATE_KEY");

        if (privateKeyBase64 == null)
        {
            throw new InvalidOperationException("Enviroment variable APACHI_PC_PRIVATE_KEY must be set.");
        }

        var privateKey = Convert.FromBase64String(privateKeyBase64);
        return privateKey;
    }
}
