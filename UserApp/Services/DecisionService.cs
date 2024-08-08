using System.Security.Cryptography;
using System.Text;
using Apachi.Shared.Crypto;
using Apachi.Shared.Factories;
using Apachi.Shared.Messages;
using Apachi.UserApp.Data;
using Apachi.ViewModels.Models;
using Apachi.ViewModels.Services;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Math;

namespace Apachi.UserApp.Services;

public class DecisionService : IDecisionService
{
    private readonly ISessionService _sessionService;
    private readonly Func<AppDbContext> _appDbContextFactory;
    private readonly Func<MessageFactory> _messageFactoryFactory;

    public DecisionService(
        ISessionService sessionService,
        Func<AppDbContext> appDbContextFactory,
        Func<MessageFactory> messageFactoryFactory
    )
    {
        _sessionService = sessionService;
        _appDbContextFactory = appDbContextFactory;
        _messageFactoryFactory = messageFactoryFactory;
    }

    public async Task<List<GradedSubmissionModel>> GetGradedSubmissionsAsync()
    {
        await using var appDbContext = _appDbContextFactory();
        var submissions = await appDbContext
            .Submissions.Where(submission => submission.SubmitterId == _sessionService.UserId!.Value)
            .ToListAsync();

        await using var messageFactory = _messageFactoryFactory();
        var models = new List<GradedSubmissionModel>();

        foreach (var submission in submissions)
        {
            var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(submission.EncryptedPrivateKey);
            var submissionKey = await _sessionService.SymmetricDecryptAndVerifyAsync(submission.EncryptedSubmissionKey);
            var publicKey = GetPublicKeyFromPrivateKey(privateKey);

            var creationMessage = await messageFactory.GetCreationMessageBySubmissionKeyAsync(submissionKey, publicKey);

            var reviewRandomness = new BigInteger(creationMessage.ReviewRandomness);
            var reviewCommitment = Commitment.Create(creationMessage.Paper, reviewRandomness);
            PaperReviewersMatchingMessage matchingMessage;

            try
            {
                matchingMessage = await messageFactory.GetMatchingMessageByCommitmentAsync(reviewCommitment.ToBytes());
            }
            catch (MessageCreationException)
            {
                continue;
            }

            var gradeAndReviewsMessage = await messageFactory.GetGradeAndReviewsMessageBySubmissionKeyAsync(
                submissionKey,
                matchingMessage.ReviewerPublicKeys
            );

            if (gradeAndReviewsMessage == null)
            {
                continue;
            }

            var paperHash = await Task.Run(() => SHA256.HashData(creationMessage.Paper));
            var grade = DeserializeOneByteArray(gradeAndReviewsMessage.Grade)[0];
            var reviewModels = new List<ReviewModel>();

            for (var i = 0; i < gradeAndReviewsMessage.Reviews.Count; i++)
            {
                var publicKeyHash = await Task.Run(() => SHA256.HashData(matchingMessage.ReviewerPublicKeys[i]));
                var hashString = Convert.ToHexString(publicKeyHash).Remove(10);
                var review = Encoding.UTF8.GetString(gradeAndReviewsMessage.Reviews[i]);

                var reviewModel = new ReviewModel(hashString, review);
                reviewModels.Add(reviewModel);
            }

            var model = new GradedSubmissionModel(submission.Id, paperHash, grade, reviewModels);
            models.Add(model);
        }

        return models;
    }

    public async Task DownloadPaperAsync(Guid submissionId, string paperFilePath)
    {
        await using var appDbContext = _appDbContextFactory();
        var submission = await appDbContext.Submissions.FirstAsync(submission => submission.Id == submissionId);

        var privateKey = await _sessionService.SymmetricDecryptAndVerifyAsync(submission.EncryptedPrivateKey);
        var submissionKey = await _sessionService.SymmetricDecryptAndVerifyAsync(submission.EncryptedSubmissionKey);
        var publicKey = GetPublicKeyFromPrivateKey(privateKey);

        await using var messageFactory = _messageFactoryFactory();
        var creationMessage = await messageFactory.GetCreationMessageBySubmissionKeyAsync(submissionKey, publicKey);
        await File.WriteAllBytesAsync(paperFilePath, creationMessage.Paper);
    }
}
