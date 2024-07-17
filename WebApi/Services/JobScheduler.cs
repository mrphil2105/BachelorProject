using Apachi.Shared;
using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Services;

public class JobScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobScheduler> _logger;

    public JobScheduler(IServiceScopeFactory scopeFactory, ILogger<JobScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var period = TimeSpan.FromSeconds(30);
        using var timer = new PeriodicTimer(period);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var jobSchedules = await dbContext.JobSchedules.ToListAsync();

            foreach (var jobSchedule in jobSchedules)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await CreateJobsForScheduleAsync(jobSchedule, dbContext);
            }
        }
    }

    private async Task CreateJobsForScheduleAsync(JobSchedule jobSchedule, AppDbContext dbContext)
    {
        if (DateTimeOffset.Now - jobSchedule.Interval < jobSchedule.LastRun)
        {
            return;
        }

        _logger.LogInformation("Scheduling jobs for job type {JobType}.", jobSchedule.JobType);
        jobSchedule.Status = JobScheduleStatus.Running;
        await dbContext.SaveChangesAsync();

        var jobs = await JobsForJobTypeAsync(jobSchedule.JobType, dbContext);
        dbContext.Jobs.AddRange(jobs);

        jobSchedule.LastRun = DateTimeOffset.Now;
        jobSchedule.Status = JobScheduleStatus.Ready;

        await dbContext.SaveChangesAsync();
    }

    private async Task<List<Job>> JobsForJobTypeAsync(JobType jobType, AppDbContext dbContext)
    {
        var jobs = new List<Job>();

        switch (jobType)
        {
            case JobType.CreateReviews:
                await JobsForCreateReviewsAsync(dbContext, jobs);
                break;
            case JobType.Matching:
                await JobsForMatchingAsync(dbContext, jobs);
                break;
            case JobType.ShareAssessments:
                await JobsForShareAssessmentsAsync(dbContext, jobs);
                break;
            default:
                throw new ArgumentException("Invalid job type specified.", nameof(jobType));
        }

        return jobs;
    }

    private async Task JobsForCreateReviewsAsync(AppDbContext dbContext, List<Job> jobs)
    {
        var submissions = dbContext
            .Submissions.Where(submission => submission.Status == SubmissionStatus.Created)
            .AsAsyncEnumerable();

        await foreach (var submission in submissions)
        {
            var shouldSchedule = await NeedJobScheduleAsync(JobType.CreateReviews, submission, dbContext);

            if (!shouldSchedule)
            {
                continue;
            }

            var job = new Job { Type = JobType.CreateReviews, Payload = submission.Id.ToString() };
            jobs.Add(job);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task JobsForMatchingAsync(AppDbContext dbContext, List<Job> jobs)
    {
        var submissions = dbContext
            .Submissions.Where(submission => submission.Status == SubmissionStatus.Matching)
            .AsAsyncEnumerable();

        await foreach (var submission in submissions)
        {
            var reviewCount = await dbContext.Reviews.CountAsync(review => review.SubmissionId == submission.Id);
            var bidCount = await dbContext.Reviews.CountAsync(review =>
                review.SubmissionId == submission.Id
                && (review.Status == ReviewStatus.Pending || review.Status == ReviewStatus.Abstain)
            );

            if (bidCount != reviewCount)
            {
                // Some reviewers have still not responded with a bid.
                continue;
            }

            var shouldSchedule = await NeedJobScheduleAsync(JobType.Matching, submission, dbContext);

            if (!shouldSchedule)
            {
                continue;
            }

            var job = new Job { Type = JobType.Matching, Payload = submission.Id.ToString() };
            jobs.Add(job);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task JobsForShareAssessmentsAsync(AppDbContext dbContext, List<Job> jobs)
    {
        var submissions = dbContext
            .Submissions.Where(submission => submission.Status == SubmissionStatus.Reviewing)
            .AsAsyncEnumerable();

        await foreach (var submission in submissions)
        {
            var reviewCount = await dbContext.Reviews.CountAsync(review => review.SubmissionId == submission.Id);
            var discussingOrAbstainCount = await dbContext.Reviews.CountAsync(review =>
                review.SubmissionId == submission.Id
                && (review.Status == ReviewStatus.Discussing || review.Status == ReviewStatus.Abstain)
            );

            if (discussingOrAbstainCount != reviewCount)
            {
                // Some reviewers have still not responded with an assessment.
                continue;
            }

            var shouldSchedule = await NeedJobScheduleAsync(JobType.ShareAssessments, submission, dbContext);

            if (!shouldSchedule)
            {
                continue;
            }

            var job = new Job { Type = JobType.ShareAssessments, Payload = submission.Id.ToString() };
            jobs.Add(job);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<bool> NeedJobScheduleAsync(JobType jobType, Submission submission, AppDbContext dbContext)
    {
        var submissionIdString = submission.Id.ToString();
        var count = await dbContext.Submissions.CountAsync();

        if (count == 0)
        {
            return true;
        }

        var hasOnlyFailedJobs = await dbContext
            .Jobs.Where(job => job.Type == jobType && job.Payload == submissionIdString)
            .AllAsync(job => job.Status == JobStatus.Failed);
        return hasOnlyFailedJobs;
    }
}
