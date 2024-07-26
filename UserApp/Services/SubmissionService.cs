using System.Security.Cryptography;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Services;
using Org.BouncyCastle.Math;

namespace Apachi.UserApp.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;
    private readonly IApiService _apiService;

    public SubmissionService(
        ISessionService sessionService,
        Func<AppDbContext> appDbContextFactory,
        Func<LogDbContext> logDbContextFactory,
        IApiService apiService
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
        _apiService = apiService;
    }

    public async Task SubmitPaperAsync(string paperFilePath)
    {
        var paperBytes = await File.ReadAllBytesAsync(paperFilePath);
        var (submissionMessage, submissionRandomness, submissionPrivateKey, submissionPublicKey) =
            await CreateSubmissionMessageAsync(paperBytes);
        var (commitmentsMessage, identityRandomness) = await CreateSubmissionIdentityCommitmentsMessageAsync(
            paperBytes,
            submissionRandomness,
            submissionPrivateKey,
            submissionPublicKey
        );

        var submissionId = Guid.NewGuid();

        await using var logDbContext = _logDbContextFactory();
        logDbContext.AddMessage(submissionId, submissionMessage);
        logDbContext.AddMessage(submissionId, commitmentsMessage);
        await logDbContext.SaveChangesAsync();

        var encryptedSubmissionPrivateKey = await _sessionService.SymmetricEncryptAsync(submissionPrivateKey);
        var encryptedIdentityRandomness = await _sessionService.SymmetricEncryptAsync(identityRandomness.ToByteArray());

        await using var appDbContext = _appDbContextFactory();
        var submission = new Submission
        {
            Id = submissionId,
            EncryptedPrivateKey = encryptedSubmissionPrivateKey,
            EncryptedIdentityRandomness = encryptedIdentityRandomness,
            SubmitterId = _sessionService.UserId!.Value
        };
        appDbContext.Submissions.Add(submission);
        await appDbContext.SaveChangesAsync();
    }

    private async Task<(
        SubmissionMessage SubmissionMessage,
        BigInteger SubmissionRandomness,
        byte[] SubmissionPrivateKey,
        byte[] submissionPublicKey
    )> CreateSubmissionMessageAsync(byte[] paperBytes)
    {
        var submissionRandomness = GenerateBigInteger();
        var reviewRandomness = GenerateBigInteger();

        var submissionKey = RandomNumberGenerator.GetBytes(32);
        var pcPublicKey = GetPCPublicKey();
        var encryptedSubmissionKey = await AsymmetricEncryptAsync(submissionKey, pcPublicKey);

        var encryptedPaper = await SymmetricEncryptAsync(paperBytes, submissionKey, null);
        var encryptedSubmissionRandomness = await SymmetricEncryptAsync(
            submissionRandomness.ToByteArray(),
            submissionKey,
            null
        );
        var encryptedReviewRandomness = await SymmetricEncryptAsync(
            reviewRandomness.ToByteArray(),
            submissionKey,
            null
        );

        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(encryptedPaper);
        await memoryStream.WriteAsync(encryptedSubmissionRandomness);
        await memoryStream.WriteAsync(encryptedReviewRandomness);
        await memoryStream.WriteAsync(encryptedSubmissionKey);
        var bytesToBeSigned = memoryStream.ToArray();

        var (submissionPrivateKey, submissionPublicKey) = await GenerateKeyPairAsync();
        var submissionSignature = await CalculateSignatureAsync(bytesToBeSigned, submissionPrivateKey);

        var submissionMessage = new SubmissionMessage(
            encryptedPaper,
            encryptedSubmissionRandomness,
            encryptedReviewRandomness,
            encryptedSubmissionKey,
            submissionSignature
        );
        return (submissionMessage, submissionRandomness, submissionPrivateKey, submissionPublicKey);
    }

    private async Task<(
        SubmissionIdentityCommitmentsMessage SubmissionIdentityCommitmentsMessage,
        BigInteger IdentityRandomness
    )> CreateSubmissionIdentityCommitmentsMessageAsync(
        byte[] paperBytes,
        BigInteger submissionRandomness,
        byte[] submissionPrivateKey,
        byte[] submissionPublicKey
    )
    {
        var idBytes = _sessionService.UserId!.Value.ToByteArray(true);

        var identityRandomness = GenerateBigInteger();
        var submissionCommitment = Commitment.Create(paperBytes, submissionRandomness);
        var identityCommitment = Commitment.Create(idBytes, identityRandomness);

        var submissionCommitmentBytes = submissionCommitment.ToBytes();
        var identityCommitmentBytes = identityCommitment.ToBytes();

        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(submissionCommitmentBytes);
        await memoryStream.WriteAsync(identityCommitmentBytes);
        var bytesToBeSigned = memoryStream.ToArray();

        var commitmentsSignature = await CalculateSignatureAsync(bytesToBeSigned, submissionPrivateKey);
        var commitmentsMessage = new SubmissionIdentityCommitmentsMessage(
            submissionCommitmentBytes,
            identityCommitmentBytes,
            commitmentsSignature,
            submissionPublicKey
        );
        return (commitmentsMessage, identityRandomness);
    }
}
