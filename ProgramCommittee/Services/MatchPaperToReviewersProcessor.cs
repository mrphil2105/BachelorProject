using System.Security.Cryptography;
using Apachi.ProgramCommittee.Data;
using Apachi.Shared.Crypto;
using Apachi.Shared.Data;
using Apachi.Shared.Data.Messages;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.ProgramCommittee.Services;

public class MatchPaperToReviewersProcessor : IJobProcessor
{
    private readonly LogDbContext _logDbContext;

    public MatchPaperToReviewersProcessor(LogDbContext logDbContext)
    {
        _logDbContext = logDbContext;
    }

    public async Task ProcessJobAsync(Job job, CancellationToken cancellationToken)
    {
        var reviewCommitment = await CreateReviewCommitmentAsync(job.SubmissionId);
        var reviewNonce = GenerateBigInteger().ToByteArray();

        var bidMessages = await _logDbContext.GetMessagesAsync<BidMessage>(job.SubmissionId);
        var reviewers = await _logDbContext.Reviewers.ToListAsync();
        var pcPrivateKey = GetPCPrivateKey();

        var reviewerPublicKeys = new List<byte[]>();

        await using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(reviewCommitment);

        foreach (var bidMessage in bidMessages)
        {
            var reviewer = await FindMatchingReviewerAsync(bidMessage, reviewers, pcPrivateKey);

            if (reviewer == null)
            {
                // Reviewer has chosen to abstain from reviewing the paper.
                continue;
            }

            reviewerPublicKeys.Add(reviewer.PublicKey);
            await memoryStream.WriteAsync(reviewer.PublicKey);
        }

        await memoryStream.WriteAsync(reviewNonce);
        var bytesToBeSigned = memoryStream.ToArray();
        var matchingSignature = await CalculateSignatureAsync(bytesToBeSigned, pcPrivateKey);

        // TODO: Add proof that the paper submission commitment and paper review commitment hides the same paper.
        var submissionReviewProof = Array.Empty<byte>();

        var matchingMessage = new PaperReviewersMatchingMessage(
            reviewCommitment,
            SerializeByteArrays(reviewerPublicKeys),
            reviewNonce,
            matchingSignature,
            submissionReviewProof
        );
        _logDbContext.AddMessage(job.SubmissionId, matchingMessage);
        await _logDbContext.SaveChangesAsync();
    }

    private async Task<byte[]> CreateReviewCommitmentAsync(Guid submissionId)
    {
        var submissionMessage = await _logDbContext.GetMessageAsync<SubmissionMessage>(submissionId);

        var pcPrivateKey = GetPCPrivateKey();
        var submissionKey = await AsymmetricDecryptAsync(submissionMessage.EncryptedSubmissionKey, pcPrivateKey);

        var paperBytes = await SymmetricDecryptAsync(submissionMessage.EncryptedPaper, submissionKey, null);
        var reviewRandomnessBytes = await SymmetricDecryptAsync(
            submissionMessage.EncryptedReviewRandomness,
            submissionKey,
            null
        );

        var reviewRandomness = new BigInteger(reviewRandomnessBytes);
        var reviewCommitment = Commitment.Create(paperBytes, reviewRandomness);
        var reviewCommitmentBytes = reviewCommitment.ToBytes();
        return reviewCommitmentBytes;
    }

    private async Task<Reviewer?> FindMatchingReviewerAsync(
        BidMessage bidMessage,
        List<Reviewer> reviewers,
        byte[] pcPrivateKey
    )
    {
        // Decrypt each and check the signature to find out if the message is from the current reviewer.
        foreach (var reviewer in reviewers)
        {
            var sharedKey = await AsymmetricDecryptAsync(reviewer.EncryptedSharedKey, pcPrivateKey);
            byte[] paperBytes;
            byte[] bidBytes;

            try
            {
                paperBytes = await SymmetricDecryptAsync(bidMessage.EncryptedPaper, sharedKey, null);
                bidBytes = await SymmetricDecryptAsync(bidMessage.EncryptedBid, sharedKey, null);
            }
            catch (CryptographicException)
            {
                // Ignore exception about invalid padding as it means the bid is not encrypted with sharedKey.
                continue;
            }

            await using var memoryStream = new MemoryStream();
            await memoryStream.WriteAsync(paperBytes);
            await memoryStream.WriteAsync(bidBytes);
            var bytesToVerify = memoryStream.ToArray();

            var isSignatureValid = await VerifySignatureAsync(bytesToVerify, bidMessage.Signature, reviewer.PublicKey);

            if (!isSignatureValid)
            {
                continue;
            }

            var wantsToReview = bidBytes[0] == 1;
            return wantsToReview ? reviewer : null;
        }

        throw new InvalidOperationException("A matching reviewer for the bid was not found.");
    }
}
