using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Calculators;

public class GroupKeyAndGradeRandomnessShareCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public GroupKeyAndGradeRandomnessShareCalculator(
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
        var matchingMessages = _messageFactory.GetMatchingMessagesAsync();

        await foreach (var matchingMessage in matchingMessages)
        {
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare
                && @event.Identifier == matchingMessage.ReviewCommitment
            );

            if (hasExisting)
            {
                continue;
            }

            var signatureMessages = _messageFactory.GetCommitmentAndNonceSignatureMessagesAsync(
                matchingMessage.ReviewerPublicKeys
            );
            var reviewCount = 0;

            await foreach (var signatureMessage in signatureMessages)
            {
                // The signature message can still be for another paper, in case the reviewer for the other paper is
                // also reviewing current paper. So we need to check the review commitment to be sure.
                if (!signatureMessage.ReviewCommitment.SequenceEqual(matchingMessage.ReviewCommitment))
                {
                    continue;
                }

                reviewCount++;
            }

            if (reviewCount != matchingMessage.ReviewerPublicKeys.Count)
            {
                // Not all reviewers have signed the review commitment and nonce.
                continue;
            }

            var creationMessage = await _messageFactory.GetCreationMessageByReviewCommitmentAsync(
                matchingMessage.ReviewCommitment
            );
            var groupKeyMessage = new GroupKeyAndGradeRandomnessShareMessage
            {
                Paper = creationMessage!.Paper,
                GroupKey = RandomNumberGenerator.GetBytes(32),
                GradeRandomness = GenerateBigInteger().ToByteArray()
            };

            var reviewers = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .ToListAsync();

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await reviewer.DecryptSharedKeyAsync();
                var groupKeyEntry = new LogEntry
                {
                    Step = ProtocolStep.GroupKeyAndGradeRandomnessShare,
                    Data = await groupKeyMessage.SerializeAsync(sharedKey)
                };
                _logDbContext.Entries.Add(groupKeyEntry);

                var logEvent = new LogEvent
                {
                    Step = groupKeyEntry.Step,
                    Identifier = matchingMessage.ReviewCommitment
                };
                _appDbContext.LogEvents.Add(logEvent);
            }
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
