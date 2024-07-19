using System.Security.Cryptography;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class ReviewerController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReviewerController> _logger;

    public ReviewerController(IConfiguration configuration, AppDbContext dbContext, ILogger<ReviewerController> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ReviewerRegisteredDto> Register([FromBody] ReviewerRegisterDto registerDto)
    {
        var programCommitteePublicKey = KeyUtils.GetPCPublicKey();
        var sharedKey = RandomNumberGenerator.GetBytes(32);

        var programCommitteeEncryptedSharedKey = await EncryptionUtils.AsymmetricEncryptAsync(
            sharedKey,
            programCommitteePublicKey
        );
        var reviewer = new Reviewer
        {
            ReviewerPublicKey = registerDto.ReviewerPublicKey,
            EncryptedSharedKey = programCommitteeEncryptedSharedKey
        };

        _dbContext.Reviewers.Add(reviewer);
        await _dbContext.SaveChangesAsync();

        var reviewerEncryptedSharedKey = await EncryptionUtils.AsymmetricEncryptAsync(
            sharedKey,
            registerDto.ReviewerPublicKey
        );
        var registeredDto = new ReviewerRegisteredDto(reviewer.Id, reviewerEncryptedSharedKey);

        _logger.LogInformation("Reviewer registered new account with id: {Id}", reviewer.Id);
        return registeredDto;
    }

    [HttpGet]
    public async Task<List<ReviewableSubmissionDto>> GetReviewableSubmissions(Guid reviewerId)
    {
        var reviewableSubmissionDtos = await _dbContext
            .Reviews.Include(review => review.Submission)
            .Where(review =>
                review.ReviewerId == reviewerId
                && (review.Status == ReviewStatus.Pending || review.Status == ReviewStatus.Discussing)
                && review.Submission.Status == SubmissionStatus.Reviewing
            )
            .Select(review => new ReviewableSubmissionDto(
                review.Submission.Id,
                review.Status,
                review.Submission.Title,
                review.Submission.Description,
                review.Submission.PaperSignature,
                review.EncryptedReviewRandomness!,
                review.Submission.ReviewRandomnessSignature,
                review.Submission.ReviewCommitment,
                review.Submission.ReviewNonce,
                review.Submission.CreatedDate
            ))
            .ToListAsync();
        return reviewableSubmissionDtos;
    }
}
