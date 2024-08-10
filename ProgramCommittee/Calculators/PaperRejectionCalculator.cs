using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.ProgramCommittee.Calculators;

public class PaperRejectionCalculator : ICalculator
{
    private const int FailingGrade = 0;

    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public PaperRejectionCalculator(AppDbContext appDbContext, LogDbContext logDbContext, MessageFactory messageFactory)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
        _messageFactory = messageFactory;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var creationMessages = _messageFactory.GetCreationMessagesAsync();

        await foreach (var creationMessage in creationMessages)
        {
            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);
            var reviewCommitmentBytes = reviewCommitment.ToBytes();

            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperRejection && @event.Identifier == reviewCommitmentBytes
            );

            if (hasExisting)
            {
                continue;
            }

            var paperHash = SHA256.HashData(creationMessage.Paper);
            var hasMatching = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperReviewersMatching && @event.Identifier == paperHash
            );

            if (!hasMatching)
            {
                continue;
            }

            var matchingMessage = await _messageFactory.GetMatchingMessageByCommitmentAsync(reviewCommitmentBytes);
            var gradeAndReviewsMessage = await _messageFactory.GetGradeAndReviewsMessageBySubmissionKeyAsync(
                creationMessage.SubmissionKey,
                matchingMessage!.ReviewerPublicKeys
            );

            if (gradeAndReviewsMessage == null)
            {
                continue;
            }

            var (grade, _) = DeserializeGrade(gradeAndReviewsMessage.Grade);

            if (grade > FailingGrade)
            {
                continue;
            }

            var reviewer = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .FirstAsync();

            var pcPrivateKey = GetPCPrivateKey();
            var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
            var gradeRandomnessMessage = await _messageFactory.GetGroupKeyAndRandomnessMessageByPaperHashAsync(
                paperHash,
                sharedKey
            );

            var rejectionMessage = new PaperRejectionMessage
            {
                ReviewCommitment = reviewCommitmentBytes,
                Grade = gradeAndReviewsMessage.Grade,
                GradeRandomness = gradeRandomnessMessage!.GradeRandomness
            };
            var rejectionEntry = new LogEntry
            {
                Step = ProtocolStep.PaperRejection,
                Data = await rejectionMessage.SerializeAsync()
            };
            _logDbContext.Entries.Add(rejectionEntry);

            var logEvent = new LogEvent { Step = rejectionEntry.Step, Identifier = reviewCommitmentBytes };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
