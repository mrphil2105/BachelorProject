using System.Security.Cryptography;
using Apachi.AvaloniaApp.Data;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.AvaloniaApp.Services;

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
        var (submissionPublicKey, submissionPrivateKey) = await KeyUtils.GenerateKeyPairAsync();
        var paperBytes = await File.ReadAllBytesAsync(paperFilePath);
        var (encryptedPaper, encryptedSubmissionKey) = await EncryptPaperAsync(paperBytes);

        var submissionRandomness = DataUtils.GenerateBigInteger();
        var reviewRandomness = DataUtils.GenerateBigInteger();
        var identityRandomness = DataUtils.GenerateBigInteger();

        var submissionRandomnessBytes = submissionRandomness.ToByteArray();
        var reviewRandomnessBytes = reviewRandomness.ToByteArray();
        var identityRandomnessBytes = identityRandomness.ToByteArray();

        var submissionCommitment = Commitment.Create(paperBytes, submissionRandomness);
        var identityCommitment = Commitment.Create(paperBytes, identityRandomness);

        var submissionCommitmentBytes = submissionCommitment.ToBytes();
        var identityCommitmentBytes = identityCommitment.ToBytes();

        // We assume the following byte arrays are part of the "message" described in step (1).
        var bytesToBeSigned = DataUtils.CombineByteArrays(
            encryptedPaper,
            encryptedSubmissionKey,
            submissionRandomnessBytes,
            reviewRandomnessBytes,
            submissionCommitmentBytes,
            identityCommitmentBytes
        );
        var submissionSignature = await KeyUtils.CalculateSignatureAsync(bytesToBeSigned, submissionPrivateKey);

        var submitDto = new SubmitDto(
            title,
            description,
            encryptedPaper,
            encryptedSubmissionKey,
            submissionRandomnessBytes,
            reviewRandomnessBytes,
            submissionCommitmentBytes,
            identityCommitmentBytes,
            submissionPublicKey,
            submissionSignature
        );

        var submission = await CreateSubmissionEntityAsync(submissionPrivateKey, identityCommitmentBytes);
        return (submitDto, submission);
    }

    private async Task<(byte[] EncryptedPaper, byte[] EncryptedSubmissionKey)> EncryptPaperAsync(byte[] paperBytes)
    {
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();
        var submissionKey = RandomNumberGenerator.GetBytes(32);

        var encryptedPaper = await EncryptionUtils.SymmetricEncryptAsync(paperBytes, submissionKey, null);
        var encryptedSubmissionKey = await EncryptionUtils.AsymmetricEncryptAsync(
            submissionKey,
            programCommitteePublicKey
        );
        return (encryptedPaper, encryptedSubmissionKey);
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
