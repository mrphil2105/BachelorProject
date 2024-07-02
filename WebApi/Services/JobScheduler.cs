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

    private Task<List<Job>> JobsForJobTypeAsync(JobType jobType, AppDbContext dbContext)
    {
        switch (jobType)
        {
            case JobType.CreateReviews:
                return JobsForCreateReviewsAsync(dbContext);
            default:
                throw new ArgumentException("Invalid job type specified.", nameof(jobType));
        }
    }

    private async Task<List<Job>> JobsForCreateReviewsAsync(AppDbContext dbContext)
    {
        var jobs = new List<Job>();
        var submissions = dbContext
            .Submissions.Where(submission => submission.Status == SubmissionStatus.Created)
            .AsAsyncEnumerable();

        await foreach (var submission in submissions)
        {
            var job = new Job { Type = JobType.CreateReviews, Payload = submission.Id.ToString() };
            jobs.Add(job);
            submission.Status = SubmissionStatus.Matching;
        }

        await dbContext.SaveChangesAsync();
        return jobs;
    }
}
