using Apachi.Shared;
using Apachi.Shared.Dtos;
using Apachi.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class ReviewController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(IConfiguration configuration, AppDbContext dbContext, ILogger<ReviewController> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<List<OpenSubmissionDto>> GetOpenSubmissions(Guid reviewerId)
    {
        var openReviews = await _dbContext
            .Reviews.Include(review => review.Submission)
            .Where(review => review.ReviewerId == reviewerId && review.Status == ReviewStatus.Open)
            .ToListAsync();

        var openSubmissionDtos = openReviews
            .Select(review => review.Submission)
            // This check is most likely not needed, since all reviews should be closed when a submission is closed.
            .Where(submission => submission.Status == SubmissionStatus.Open)
            .Select(submission => new OpenSubmissionDto(
                submission.Id,
                submission.Status,
                submission.PaperSignature,
                submission.CreatedDate
            ))
            .ToList();

        return openSubmissionDtos;
    }

    [HttpGet]
    public IActionResult GetPaper(Guid submissionId, Guid reviewerId)
    {
        var reviewsDirectoryPath = _configuration.GetReviewsStorage();
        var submissionDirectoryPath = Path.Combine(reviewsDirectoryPath, submissionId.ToString());
        var encryptedPaperFilePath = Path.Combine(submissionDirectoryPath, reviewerId.ToString());

        var fileStream = System.IO.File.OpenRead(encryptedPaperFilePath);
        return File(fileStream, "application/octet-stream");
    }

    [HttpPost]
    public async Task<IActionResult> CreateBid([FromBody] BidDto bidDto)
    {
        var submission = await _dbContext.Submissions.FirstOrDefaultAsync(submission =>
            submission.Id == bidDto.SubmissionId
        );
        var review = await _dbContext.Reviews.FirstOrDefaultAsync(review =>
            review.SubmissionId == bidDto.SubmissionId && review.ReviewerId == bidDto.ReviewerId
        );

        if (submission == null || review == null)
        {
            return NotFound();
        }

        if (submission.Status != SubmissionStatus.Open || review.Status != ReviewStatus.Open)
        {
            return BadRequest();
        }

        review.Status = bidDto.WantsToReview ? ReviewStatus.Pending : ReviewStatus.Abstain;
        await _dbContext.SaveChangesAsync();
        return Ok();
    }
}
