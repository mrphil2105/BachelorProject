using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Apachi.AvaloniaApp.Data;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.ViewModels.Services;

namespace Apachi.AvaloniaApp.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _dbContextFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public SubmissionService(
        ISessionService sessionService,
        Func<AppDbContext> dbContextFactory,
        IHttpClientFactory httpClientFactory
    )
    {
        _sessionService = sessionService;
        _dbContextFactory = dbContextFactory;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SubmitPaperAsync(string paperFilePath)
    {
        var (submitDto, submission) = await CreateSubmissionModelsAsync(paperFilePath);
        var submitJson = JsonSerializer.Serialize(submitDto);
        var jsonContent = new StringContent(submitJson, Encoding.UTF8, "application/json");
        var httpClient = _httpClientFactory.CreateClient();

        using var response = await httpClient.PostAsync("Submission/Create", jsonContent);
        var submittedJson = await response.Content.ReadAsStringAsync();
        var submittedDto = JsonSerializer.Deserialize<SubmittedDto>(submittedJson)!;
        submission.Id = submittedDto.SubmissionId;
        submission.SubmissionCommitmentSignature = submittedDto.SubmissionCommitmentSignature;

        await using var dbContext = _dbContextFactory();
        submission.SubmitterId = _sessionService.UserId!.Value;
        dbContext.Submissions.Add(submission);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    // See Apachi Chapter 5.2.1
    private async Task<(SubmitDto SubmitDto, Submission Submission)> CreateSubmissionModelsAsync(string paperFilePath)
    {
        var (submissionPublicKey, submissionPrivateKey) = await Task.Run(KeyUtils.GenerateKeyPair)
            .ConfigureAwait(false);
        var (encryptedPaper, encryptedSubmissionKey) = await EncryptPaperAsync(paperFilePath).ConfigureAwait(false);

        var submissionRandomness = DataUtils.GenerateBigInteger();
        var reviewRandomness = DataUtils.GenerateBigInteger();
        var identityRandomness = DataUtils.GenerateBigInteger();

        var submissionRandomnessBytes = submissionRandomness.ToByteArray();
        var reviewRandomnessBytes = reviewRandomness.ToByteArray();
        var identityRandomnessBytes = identityRandomness.ToByteArray();

        var submissionCommitment = Commitment.Create(encryptedPaper, submissionRandomness);
        var identityCommitment = Commitment.Create(encryptedPaper, identityRandomness);

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
        var submissionSignature = KeyUtils.CalculateSignature(bytesToBeSigned, submissionPrivateKey);

        var submitDto = new SubmitDto(
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

    private async Task<(byte[] EncryptedPaper, byte[] EncryptedSubmissionKey)> EncryptPaperAsync(string paperFilePath)
    {
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();
        var submissionKey = RandomNumberGenerator.GetBytes(32);

        var fileStream = File.OpenRead(paperFilePath);
        var encryptedPaper = await EncryptionUtils.SymmetricEncryptAsync(fileStream, submissionKey, null);

        var encryptedSubmissionKey = await Task.Run(
            () => EncryptionUtils.AsymmetricEncrypt(submissionKey, programCommitteePublicKey)
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
