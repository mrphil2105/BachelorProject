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

public class PaperAndReviewRandomnessShareCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public PaperAndReviewRandomnessShareCalculator(
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
            var paperHash = SHA256.HashData(creationMessage.Paper);
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperAndReviewRandomnessShare && @event.Identifier == paperHash
            );

            if (hasExisting)
            {
                continue;
            }

            var hasMatching = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperReviewersMatching && @event.Identifier == paperHash
            );

            if (!hasMatching)
            {
                continue;
            }

            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);
            var matchingMessage = await _messageFactory.GetMatchingMessageByCommitmentAsync(reviewCommitment.ToBytes());

            var pcPrivateKey = GetPCPrivateKey();
            var reviewers = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .ToListAsync();

            var paperMessage = new PaperAndReviewRandomnessShareMessage
            {
                Paper = creationMessage.Paper,
                ReviewRandomness = creationMessage.ReviewRandomness
            };

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
                var paperEntry = new LogEntry
                {
                    Step = ProtocolStep.PaperAndReviewRandomnessShare,
                    Data = await paperMessage.SerializeAsync(sharedKey)
                };
                _logDbContext.Entries.Add(paperEntry);

                var logEvent = new LogEvent { Step = paperEntry.Step, Identifier = paperHash };
                _appDbContext.LogEvents.Add(logEvent);
            }
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
