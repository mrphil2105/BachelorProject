using Apachi.ProgramCommittee.Data;
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
        var period = TimeSpan.FromSeconds(30);
        using var timer = new PeriodicTimer(period);

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
        {
            await using var lifetimeScope = _container.BeginLifetimeScope();
            var dbContext = lifetimeScope.Resolve<AppDbContext>();
            var jobSchedules = await dbContext.JobSchedules.ToListAsync();

            foreach (var jobSchedule in jobSchedules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await CreateJobsForScheduleAsync(jobSchedule, dbContext);
            }
        }
    }

    private async Task CreateJobsForScheduleAsync(JobSchedule jobSchedule, AppDbContext dbContext)
    {
        if (DateTime.UtcNow - jobSchedule.Interval < jobSchedule.LastRun)
        {
            return;
        }

        _logger.Information("Scheduling jobs for job type {JobType}.", jobSchedule.JobType);
        jobSchedule.Status = JobScheduleStatus.Running;
        await dbContext.SaveChangesAsync();

        var jobs = await JobsForJobTypeAsync(jobSchedule.JobType, dbContext);
        dbContext.Jobs.AddRange(jobs);

        jobSchedule.LastRun = DateTime.UtcNow;
        jobSchedule.Status = JobScheduleStatus.Ready;

        await dbContext.SaveChangesAsync();
    }

    private async Task<List<Job>> JobsForJobTypeAsync(JobType jobType, AppDbContext dbContext)
    {
        var jobs = new List<Job>();

        switch (jobType)
        {
            default:
                throw new ArgumentException("Invalid job type specified.", nameof(jobType));
        }

        return jobs;
    }
}
