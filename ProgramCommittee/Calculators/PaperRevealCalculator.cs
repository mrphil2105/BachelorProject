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

public class PaperRevealCalculator : ICalculator
{
    private const int FailingGrade = 0;

    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public PaperRevealCalculator(AppDbContext appDbContext, LogDbContext logDbContext, MessageFactory messageFactory)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
        _messageFactory = messageFactory;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var hasRevealed = await _appDbContext.LogEvents.AnyAsync(@event => @event.Step == ProtocolStep.PaperReveal);

        if (hasRevealed)
        {
            return;
        }

        var hasAcceptedGrades = await _appDbContext.LogEvents.AnyAsync(@event =>
            @event.Step == ProtocolStep.AcceptedGrades
        );

        if (!hasAcceptedGrades)
        {
            return;
        }

        var creationMessages = _messageFactory.GetCreationMessagesAsync();

        await foreach (var creationMessage in creationMessages)
        {
            var paperHash = SHA256.HashData(creationMessage.Paper);
            var hasMatching = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperReviewersMatching && @event.Identifier == paperHash
            );

            if (!hasMatching)
            {
                return;
            }

            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);
            var matchingMessage = await _messageFactory.GetMatchingMessageByCommitmentAsync(reviewCommitment.ToBytes());

            var gradeAndReviewsMessage = await _messageFactory.GetGradeAndReviewsMessageBySubmissionKeyAsync(
                creationMessage.SubmissionKey,
                matchingMessage.ReviewerPublicKeys
            );

            var (grade, _) = DeserializeGrade(gradeAndReviewsMessage!.Grade);

            if (grade <= FailingGrade)
            {
                continue;
            }

            var revealMessage = new PaperRevealMessage
            {
                Paper = creationMessage.Paper,
                SubmissionRandomness = creationMessage.SubmissionRandomness,
                // TODO: Add proof that the grade is in the set of all accepted grades G_a.
                MembershipProof = Array.Empty<byte>()
            };
            var revealEntry = new LogEntry
            {
                Step = ProtocolStep.PaperReveal,
                Data = await revealMessage.SerializeAsync()
            };
            _logDbContext.Entries.Add(revealEntry);

            var logEvent = new LogEvent { Step = revealEntry.Step, Identifier = paperHash };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
