using System.Security.Cryptography;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Messages;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Services;

namespace Apachi.UserApp.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<LogDbContext> _logDbContextFactory;

    public SubmissionService(
        ISessionService sessionService,
        Func<AppDbContext> appDbContextFactory,
        Func<LogDbContext> logDbContextFactory
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _logDbContextFactory = logDbContextFactory;
    }

    public async Task SubmitPaperAsync(string paperFilePath)
    {
        var paperBytes = await File.ReadAllBytesAsync(paperFilePath);
        var (submissionPrivateKey, submissionPublicKey) = await GenerateKeyPairAsync();

        var submissionRandomness = GenerateBigInteger();
        var creationMessage = new SubmissionCreationMessage
        {
            Paper = paperBytes,
            SubmissionRandomness = submissionRandomness.ToByteArray(),
            ReviewRandomness = GenerateBigInteger().ToByteArray(),
            SubmissionKey = RandomNumberGenerator.GetBytes(32)
        };

        var idBytes = _sessionService.UserId!.Value.ToByteArray(true);
        var identityRandomness = GenerateBigInteger();

        var submissionCommitment = Commitment.Create(paperBytes, submissionRandomness);
        var identityCommitment = Commitment.Create(idBytes, identityRandomness);

        var commitmentsAndKeyMessage = new SubmissionCommitmentsAndPublicKeyMessage
        {
            SubmissionCommitment = submissionCommitment.ToBytes(),
            IdentityCommitment = identityCommitment.ToBytes(),
            SubmissionPublicKey = submissionPublicKey
        };

        var creationEntry = new LogEntry
        {
            Step = ProtocolStep.SubmissionCreation,
            Data = await creationMessage.SerializeAsync(submissionPrivateKey)
        };
        var commitmentsAndKeyEntry = new LogEntry
        {
            Step = ProtocolStep.SubmissionCommitmentsAndPublicKey,
            Data = await commitmentsAndKeyMessage.SerializeAsync(submissionPrivateKey)
        };

        await using var logDbContext = _logDbContextFactory();
        logDbContext.Entries.Add(creationEntry);
        logDbContext.Entries.Add(commitmentsAndKeyEntry);
        await logDbContext.SaveChangesAsync();

        var identityRandomnessBytes = identityRandomness.ToByteArray();
        var encryptedSubmissionPrivateKey = await _sessionService.SymmetricEncryptAndMacAsync(submissionPrivateKey);
        var encryptedIdentityRandomness = await _sessionService.SymmetricEncryptAndMacAsync(identityRandomnessBytes);

        await using var appDbContext = _appDbContextFactory();
        var submission = new Submission
        {
            EncryptedPrivateKey = encryptedSubmissionPrivateKey,
            EncryptedIdentityRandomness = encryptedIdentityRandomness,
            SubmitterId = _sessionService.UserId!.Value
        };
        appDbContext.Submissions.Add(submission);
        await appDbContext.SaveChangesAsync();
    }
}
