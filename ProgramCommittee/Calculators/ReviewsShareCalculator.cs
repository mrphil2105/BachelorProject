using System.Diagnostics;
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

public class ReviewsShareCalculator : ICalculator
{
    private readonly AppDbContext _appDbContext;
    private readonly LogDbContext _logDbContext;
    private readonly MessageFactory _messageFactory;

    public ReviewsShareCalculator(AppDbContext appDbContext, LogDbContext logDbContext, MessageFactory messageFactory)
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
                @event.Step == ProtocolStep.ReviewsShare && @event.Identifier == reviewCommitmentBytes
            );

            if (hasExisting)
            {
                continue;
            }

            var shareCount = await _appDbContext.LogEvents.CountAsync(@event =>
                @event.Step == ProtocolStep.GroupKeyAndGradeRandomnessShare
                && @event.Identifier == reviewCommitmentBytes
            );

            if (shareCount == 0)
            {
                // A group key has not been created yet. This could also mean a matching has not been made either.
                continue;
            }

            PaperReviewersMatchingMessage matchingMessage = await _messageFactory.GetMatchingMessageByCommitmentAsync(
                reviewCommitmentBytes
            );

            if (shareCount != matchingMessage.ReviewerPublicKeys.Count)
            {
                // The group key has not been shared with all reviewers yet.
                continue;
            }

            var pcPrivateKey = GetPCPrivateKey();
            var paperHash = SHA256.HashData(creationMessage.Paper);

            var reviewers = await _logDbContext
                .Reviewers.Where(reviewer => matchingMessage.ReviewerPublicKeys.Any(key => key == reviewer.PublicKey))
                .ToListAsync();

            var reviews = new List<byte[]>();
            var reviewSignatures = new List<byte[]>();
            byte[]? groupKey = null;

            foreach (var reviewer in reviewers)
            {
                var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
                var reviewMessage = await _messageFactory.GetReviewMessageByPaperHashAsync(
                    paperHash,
                    sharedKey,
                    reviewer.PublicKey
                );
                reviews.Add(reviewMessage.Review);
                reviewSignatures.Add(reviewMessage.ReviewSignature!);

                if (groupKey == null)
                {
                    var groupKeyMessage = await _messageFactory.GetGroupKeyAndRandomnessMessageByPaperHashAsync(
                        paperHash,
                        sharedKey
                    );
                    groupKey = groupKeyMessage.GroupKey;
                }
            }

            (reviews, reviewSignatures) = await ReorderReviewsAsync(
                reviews,
                reviewSignatures,
                matchingMessage.ReviewerPublicKeys
            );

            var reviewsMessage = new ReviewsShareMessage { Reviews = reviews };
            var reviewsEntry = new LogEntry
            {
                Step = ProtocolStep.ReviewsShare,
                Data = await reviewsMessage.SerializeAsync(reviewSignatures, groupKey!)
            };
            _logDbContext.Entries.Add(reviewsEntry);

            var logEvent = new LogEvent { Step = reviewsEntry.Step, Identifier = reviewCommitmentBytes };
            _appDbContext.LogEvents.Add(logEvent);
        }

        await _logDbContext.SaveChangesAsync();
        await _appDbContext.SaveChangesAsync();
    }

    private async Task<(List<byte[]>, List<byte[]>)> ReorderReviewsAsync(
        List<byte[]> reviews,
        List<byte[]> reviewSignatures,
        List<byte[]> reviewerPublicKeys
    )
    {
        Debug.Assert(reviews.Count == reviewSignatures.Count);
        Debug.Assert(reviews.Count == reviewerPublicKeys.Count);
        Debug.Assert(reviewSignatures.Count == reviewerPublicKeys.Count);

        var orderedReviews = new List<byte[]>();
        var orderedReviewSignatures = new List<byte[]>();

        foreach (var reviewerPublicKey in reviewerPublicKeys)
        {
            for (var i = 0; i < reviews.Count; i++)
            {
                var isSignatureValid = await VerifySignatureAsync(reviews[i], reviewSignatures[i], reviewerPublicKey);

                if (isSignatureValid)
                {
                    orderedReviews.Add(reviews[i]);
                    orderedReviewSignatures.Add(reviewSignatures[i]);
                    break;
                }
            }
        }

        return (orderedReviews, orderedReviewSignatures);
    }
}
