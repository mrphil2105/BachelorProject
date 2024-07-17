using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Services;

namespace Apachi.UserApp.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _dbContextFactory;
    private readonly IApiService _apiService;

    public SubmissionService(
        ISessionService sessionService,
        Func<AppDbContext> dbContextFactory,
        IApiService apiService
    )
    {
        _sessionService = sessionService;
        _dbContextFactory = dbContextFactory;
        _apiService = apiService;
    }

    public async Task SubmitPaperAsync(string title, string description, string paperFilePath)
    {
        var (submitDto, submission) = await CreateSubmissionModelsAsync(title, description, paperFilePath);
        var submittedDto = await _apiService.PostAsync<SubmitDto, SubmittedDto>("Submission/Create", submitDto);

        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();
        var isSignatureValid = await KeyUtils.VerifySignatureAsync(
            submitDto.SubmissionCommitment,
            submittedDto.SubmissionCommitmentSignature,
            programCommitteePublicKey
        );

        if (!isSignatureValid)
        {
            throw new CryptographicException("The received submission commitment signature is invalid.");
        }

        submission.Id = submittedDto.SubmissionId;
        submission.SubmissionCommitmentSignature = submittedDto.SubmissionCommitmentSignature;

        await using var dbContext = _dbContextFactory();
        submission.SubmitterId = _sessionService.UserId!.Value;
        dbContext.Submissions.Add(submission);
        await dbContext.SaveChangesAsync();
    }

    // See Apachi Chapter 5.2.1
    private async Task<(SubmitDto SubmitDto, Submission Submission)> CreateSubmissionModelsAsync(
        string title,
        string description,
        string paperFilePath
    )
    {
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();
        var (submissionPublicKey, submissionPrivateKey) = await KeyUtils.GenerateKeyPairAsync();
        var submissionKey = RandomNumberGenerator.GetBytes(32);

        var encryptedSubmissionKey = await EncryptionUtils.AsymmetricEncryptAsync(
            submissionKey,
            programCommitteePublicKey
        );

        var paperBytes = await File.ReadAllBytesAsync(paperFilePath);
        var encryptedPaper = await EncryptionUtils.SymmetricEncryptAsync(paperBytes, submissionKey, null);

        var submissionRandomness = DataUtils.GenerateBigInteger();
        var reviewRandomness = DataUtils.GenerateBigInteger();
        var identityRandomness = DataUtils.GenerateBigInteger();

        var submissionRandomnessBytes = submissionRandomness.ToByteArray();
        var reviewRandomnessBytes = reviewRandomness.ToByteArray();
        var identityRandomnessBytes = identityRandomness.ToByteArray();

        var encryptedSubmissionRandomness = await EncryptionUtils.SymmetricEncryptAsync(
            submissionRandomnessBytes,
            submissionKey,
            null
        );
        var encryptedReviewRandomness = await EncryptionUtils.SymmetricEncryptAsync(
            reviewRandomnessBytes,
            submissionKey,
            null
        );

        var submissionCommitment = Commitment.Create(paperBytes, submissionRandomness);
        var identityCommitment = Commitment.Create(paperBytes, identityRandomness);

        var submissionCommitmentBytes = submissionCommitment.ToBytes();
        var identityCommitmentBytes = identityCommitment.ToBytes();

        // We assume the following byte arrays are part of the "message" described in step (1).
        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(encryptedPaper);
        await memoryStream.WriteAsync(encryptedSubmissionKey);
        await memoryStream.WriteAsync(encryptedSubmissionRandomness);
        await memoryStream.WriteAsync(encryptedReviewRandomness);
        await memoryStream.WriteAsync(submissionCommitmentBytes);
        await memoryStream.WriteAsync(identityCommitmentBytes);
        var bytesToBeSigned = memoryStream.ToArray();

        var submissionSignature = await KeyUtils.CalculateSignatureAsync(bytesToBeSigned, submissionPrivateKey);

        var submitDto = new SubmitDto(
            title,
            description,
            encryptedPaper,
            encryptedSubmissionKey,
            encryptedSubmissionRandomness,
            encryptedReviewRandomness,
            submissionCommitmentBytes,
            identityCommitmentBytes,
            submissionPublicKey,
            submissionSignature
        );

        var submission = await CreateSubmissionEntityAsync(submissionPrivateKey, identityCommitmentBytes);
        return (submitDto, submission);
    }

    private async Task<Submission> CreateSubmissionEntityAsync(byte[] submissionPrivateKey, byte[] identityRandomness)
    {
        var encryptedSubmissionPrivateKey = await _sessionService.SymmetricEncryptAsync(submissionPrivateKey);
        var encryptedIdentityRandomness = await _sessionService.SymmetricEncryptAsync(identityRandomness);

        var submission = new Submission
        {
            EncryptedPrivateKey = encryptedSubmissionPrivateKey,
            EncryptedIdentityRandomness = encryptedIdentityRandomness
        };
        return submission;
    }
}
