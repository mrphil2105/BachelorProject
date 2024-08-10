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

public class GradeAndReviewsShareCalculator : ICalculator
{
    private static readonly int[] _validGrades = new[] { -3, 0, 2, 4, 7, 10, 12 };

    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public GradeAndReviewsShareCalculator(
        AppDbContext appDbContext,
        LogDbContext logDbContext,
        MessageFactory messageFactory
    )
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
                @event.Step == ProtocolStep.GradeAndReviewsShare && @event.Identifier == reviewCommitmentBytes
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

            var hasGroupKey = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare
                && @event.Identifier == reviewCommitmentBytes
            );

            if (!hasGroupKey)
            {
                continue;
            }

            var pcPrivateKey = GetPCPrivateKey();
            byte[]? groupKey = null;

            var matchingMessage = await _messageFactory.GetMatchingMessageByCommitmentAsync(reviewCommitmentBytes);
            var reviewers = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .ToListAsync();
            var gradeAndNonces = new List<byte[]>();

            foreach (var reviewer in reviewers)
            {
                if (groupKey == null)
                {
                    // The same group key has been sent to all reviewers in the matching, so retrieve it using the first
                    // reviewer's shared key.
                    var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
                    var groupKeyMessage = await _messageFactory.GetGroupKeyAndRandomnessMessageByPaperHashAsync(
                        paperHash,
                        sharedKey
                    );
                    groupKey = groupKeyMessage.GroupKey;
                }

                var gradeMessage = await _messageFactory.GetGradeMessageByGroupKeyAsync(groupKey, reviewer.PublicKey);

                if (gradeMessage == null)
                {
                    // Reviewer has not sent a grade yet.
                    break;
                }

                gradeAndNonces.Add(gradeMessage.Grade);
            }

            if (gradeAndNonces.Count != matchingMessage.ReviewerPublicKeys.Count)
            {
                // Not all reviewers have sent their grades yet.
                continue;
            }

            var reviewsMessage = await _messageFactory.GetReviewsMessageByGroupKeyAsync(
                groupKey!,
                matchingMessage.ReviewerPublicKeys
            );
            var closestAverageGrade = CalculateClosestAverageGrade(gradeAndNonces);

            var gradeAndReviewsMessage = new GradeAndReviewsShareMessage
            {
                Grade = closestAverageGrade,
                Reviews = reviewsMessage!.Reviews
            };
            var gradeAndReviewsData = await gradeAndReviewsMessage.SerializeAsync(
                reviewsMessage.ReviewSignatures!,
                creationMessage.SubmissionKey
            );
            var gradeAndReviewsEntry = new LogEntry
            {
                Step = ProtocolStep.GradeAndReviewsShare,
                Data = gradeAndReviewsData
            };
            _logDbContext.Entries.Add(gradeAndReviewsEntry);

            var logEvent = new LogEvent { Step = gradeAndReviewsEntry.Step, Identifier = reviewCommitmentBytes };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }

    private static byte[] CalculateClosestAverageGrade(List<byte[]> gradeAndNonces)
    {
        var averageGrade = gradeAndNonces.Average(serialized => DeserializeGrade(serialized).Grade);
        var closestGrade = _validGrades.OrderBy(grade => Math.Abs(averageGrade - grade)).First();

        var gradeNonce = GenerateBigInteger().ToByteArray();
        var gradeAndNonce = SerializeGrade(closestGrade, gradeNonce);
        return gradeAndNonce;
    }
}
