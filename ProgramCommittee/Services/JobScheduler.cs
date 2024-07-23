using Apachi.ProgramCommittee.Data;
using Apachi.Shared.Data;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Apachi.ProgramCommittee.Services;

public class JobScheduler
{
    private readonly ILifetimeScope _container;
    private readonly ILogger _logger;

    public JobScheduler(ILifetimeScope container, ILogger logger)
    {
        _container = container;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromSeconds(10);
        using var timer = new PeriodicTimer(period);

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
        {
            await using var lifetimeScope = _container.BeginLifetimeScope();
            var appDbContext = lifetimeScope.Resolve<AppDbContext>();
            var logDbContext = lifetimeScope.Resolve<LogDbContext>();
            var jobSchedules = await appDbContext.JobSchedules.ToListAsync();

            foreach (var jobSchedule in jobSchedules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CreateJobsForScheduleAsync(jobSchedule, appDbContext, logDbContext);
            }
        }
    }

    private async Task CreateJobsForScheduleAsync(
        JobSchedule jobSchedule,
        AppDbContext appDbContext,
        LogDbContext logDbContext
    )
    {
        if (DateTime.UtcNow - jobSchedule.Interval < jobSchedule.LastRun)
        {
            return;
        }

        _logger.Information("Scheduling jobs for job type {JobType}.", jobSchedule.JobType);
        jobSchedule.Status = JobScheduleStatus.Running;
        await appDbContext.SaveChangesAsync();

        var jobs = await JobsForJobTypeAsync(jobSchedule.JobType, appDbContext, logDbContext);
        appDbContext.Jobs.AddRange(jobs);

        jobSchedule.LastRun = DateTime.UtcNow;
        jobSchedule.Status = JobScheduleStatus.Ready;

        await appDbContext.SaveChangesAsync();
    }

    private static async Task<List<Job>> JobsForJobTypeAsync(
        JobType jobType,
        AppDbContext appDbContext,
        LogDbContext logDbContext
    )
    {
        var jobs = new List<Job>();

        switch (jobType)
        {
            default:
                var jobsToAdd = await JobsForGenericJobAsync(jobType, appDbContext, logDbContext);
                jobs.AddRange(jobsToAdd);
                break;
        }

        return jobs;
    }

    private static async Task<List<Job>> JobsForGenericJobAsync(
        JobType jobType,
        AppDbContext appDbContext,
        LogDbContext logDbContext
    )
    {
        var jobs = new List<Job>();
        var step = ProtocolStepForJobType(jobType);
        var submissionIds = await logDbContext.Entries.Select(entry => entry.SubmissionId).Distinct().ToListAsync();

        foreach (var submissionId in submissionIds)
        {
            var maxEntry = await logDbContext
                .Entries.Where(entry => entry.SubmissionId == submissionId)
                .OrderByDescending(entry => entry.Step)
                .FirstAsync();

            if (maxEntry.Step != step)
            {
                continue;
            }

            var shouldSchedule = await NeedJobScheduleAsync(jobType, submissionId, appDbContext);

            if (!shouldSchedule)
            {
                continue;
            }

            var job = new Job { SubmissionId = submissionId, Type = jobType };
            jobs.Add(job);
        }

        return jobs;
    }

    private static async Task<bool> NeedJobScheduleAsync(JobType jobType, Guid submissionId, AppDbContext appDbContext)
    {
        var hasAnyJobs = await appDbContext.Jobs.AnyAsync(job =>
            job.Type == jobType && job.SubmissionId == submissionId
        );

        if (!hasAnyJobs)
        {
            return true;
        }

        var hasOnlyFailedJobs = await appDbContext
            .Jobs.Where(job => job.Type == jobType && job.SubmissionId == submissionId)
            .AllAsync(job => job.Status == JobStatus.Failed);
        return hasOnlyFailedJobs;
    }

    // Returns the protocol step that is required to exist for a job to be scheduled.
    private static ProtocolStep ProtocolStepForJobType(JobType jobType)
    {
        return jobType switch
        {
            JobType.SignSubmissionCommitment => ProtocolStep.SubmissionIdentityCommitments,
            JobType.SharePaperWithReviewers => ProtocolStep.SubmissionCommitmentSignature,
            _ => throw new ArgumentException("Invalid job type specified.", nameof(jobType))
        };
    }
}
