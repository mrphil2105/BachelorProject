using System.Security.Cryptography;
using Apachi.Shared;
using Apachi.Shared.Crypto;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Apachi.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class ReviewerController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly JobScheduler _jobScheduler;
    private readonly ILogger<SubmissionController> _logger;

    public ReviewerController(
        IConfiguration configuration,
        AppDbContext dbContext,
        JobScheduler jobScheduler,
        ILogger<SubmissionController> logger
    )
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ReviewerRegisteredDto> Register([FromBody] ReviewerRegisterDto registerDto)
    {
        var programCommitteePublicKey = KeyUtils.GetProgramCommitteePublicKey();
        var sharedKey = RandomNumberGenerator.GetBytes(32);

        var programCommitteeEncryptedSharedKey = EncryptionUtils.AsymmetricEncrypt(
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

        var openSubmissions = _dbContext
            .Submissions.Where(submission => submission.Status == SubmissionStatus.Open)
            .AsAsyncEnumerable();

        await foreach (var openSubmission in openSubmissions)
        {
            await _jobScheduler.ScheduleJobAsync(JobType.CreateReviews, openSubmission.Id.ToString());
        }

        var reviewerEncryptedSharedKey = EncryptionUtils.AsymmetricEncrypt(sharedKey, registerDto.ReviewerPublicKey);
        var registeredDto = new ReviewerRegisteredDto(reviewer.Id, reviewerEncryptedSharedKey);

        _logger.LogInformation("Reviewer registered new account with id: {Id}", reviewer.Id);
        return registeredDto;
    }
}
