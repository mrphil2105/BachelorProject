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

public class AcceptedGradesCalculator : ICalculator
{
    private const int FailingGrade = 0;

    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public AcceptedGradesCalculator(AppDbContext appDbContext, LogDbContext logDbContext, MessageFactory messageFactory)
    {
        _appDbContext = appDbContext;
        _logDbContext = logDbContext;
        _messageFactory = messageFactory;
    }

    public async Task CalculateAsync(CancellationToken cancellationToken)
    {
        var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event => @event.Step == ProtocolStep.AcceptedGrades);

        if (hasExisting)
        {
            return;
        }

        var confirmationCount = await _logDbContext.Entries.CountAsync(entry =>
            entry.Step == ProtocolStep.SubmissionCommitmentSignature
        );

        if (confirmationCount == 0)
        {
            return;
        }

        var gradedCount = 0;

        var creationMessages = _messageFactory.GetCreationMessagesAsync();
        var gradeAndNonces = new List<byte[]>();

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
                matchingMessage!.ReviewerPublicKeys
            );

            if (gradeAndReviewsMessage == null)
            {
                return;
            }

            var (grade, _) = DeserializeGrade(gradeAndReviewsMessage.Grade);

            if (grade <= FailingGrade)
            {
                // Still count the paper as graded, but we avoid adding it to the list of accepted grades.
                gradedCount++;
                continue;
            }

            gradeAndNonces.Add(gradeAndReviewsMessage.Grade);
            gradedCount++;
        }

        if (gradedCount != confirmationCount)
        {
            return;
        }

        var gradesMessage = new AcceptedGradesMessage { Grades = gradeAndNonces };
        var gradesEntry = new LogEntry
        {
            Step = ProtocolStep.AcceptedGrades,
            Data = await gradesMessage.SerializeAsync()
        };
        _logDbContext.Entries.Add(gradesEntry);

        var logEvent = new LogEvent { Step = gradesEntry.Step, Identifier = Array.Empty<byte>() };
        _appDbContext.LogEvents.Add(logEvent);

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
