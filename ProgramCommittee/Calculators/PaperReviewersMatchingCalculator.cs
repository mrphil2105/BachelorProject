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

public class PaperReviewersMatchingCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public PaperReviewersMatchingCalculator(
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
        var reviewers = await _logDbContext.Reviewers.ToListAsync();
        var creationMessages = _messageFactory.GetCreationMessagesAsync();

        await foreach (var creationMessage in creationMessages)
        {
            var paperHash = SHA256.HashData(creationMessage.Paper);
            var hasExisting = await _appDbContext.LogEvents.AnyAsync(@event =>
                @event.Step == ProtocolStep.PaperReviewersMatching && @event.Identifier == paperHash
            );

            if (hasExisting)
            {
                continue;
            }

            var reviewerCount = await _appDbContext.LogEvents.CountAsync(@event =>
                @event.Step == ProtocolStep.PaperShare && @event.Identifier == paperHash
            );

            if (reviewerCount == 0)
            {
                // The paper has not been shared with any reviewers yet.
                continue;
            }

            var bidCount = 0;
            var reviewerPublicKeys = new List<byte[]>();

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await reviewer.DecryptSharedKeyAsync();
                var bidMessage = await _messageFactory.GetBidMessageByPaperHashAsync(
                    paperHash,
                    sharedKey,
                    reviewer.PublicKey
                );

                if (bidMessage == null)
                {
                    // Reviewer has not bid on the paper yet.
                    continue;
                }

                bidCount++;

                if (bidMessage.Bid[0] == 0)
                {
                    // Reviewer has chosen to abstain from reviewing the paper.
                    continue;
                }

                reviewerPublicKeys.Add(reviewer.PublicKey);
            }

            if (bidCount != reviewerCount)
            {
                // Not all reviewers have sent their bid yet.
                continue;
            }

            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);

            var matchingMessage = new PaperReviewersMatchingMessage
            {
                ReviewCommitment = reviewCommitment.ToBytes(),
                ReviewerPublicKeys = reviewerPublicKeys,
                ReviewNonce = GenerateBigInteger().ToByteArray(),
                // TODO: Add proof that the paper submission commitment and paper review commitment hide the same paper.
                EqualityProof = Array.Empty<byte>()
            };
            var matchingEntry = new LogEntry
            {
                Step = ProtocolStep.PaperReviewersMatching,
                Data = await matchingMessage.SerializeAsync()
            };
            _logDbContext.Entries.Add(matchingEntry);

            var logEvent = new LogEvent { Step = matchingEntry.Step, Identifier = paperHash };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }
}
