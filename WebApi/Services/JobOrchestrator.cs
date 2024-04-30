using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Services;

public class JobOrchestrator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobOrchestrator> _logger;

    public JobOrchestrator(IServiceScopeFactory scopeFactory, ILogger<JobOrchestrator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromSeconds(30);
        using var timer = new PeriodicTimer(period);

        while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var scheduledJobs = dbContext.Jobs.Where(job => job.Status == JobStatus.Scheduled).AsAsyncEnumerable();

            await foreach (var job in scheduledJobs)
            {
                job.Status = JobStatus.Running;
                job.StartDate = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync();

                try
                {
                    var processor = scope.ServiceProvider.GetKeyedService<IJobProcessor>(job.Type);

                    if (processor == null)
                    {
                        job.Result = "No job processor is defined for the job type.";
                        job.Status = JobStatus.Failed;
                    }
                    else
                    {
                        job.Result = await processor.ProcessJobAsync(job, cancellationToken);
                        job.Status = JobStatus.Completed;
                    }
                }
                catch (Exception exception)
                {
                    job.Result = exception.Message;
                    job.Status = JobStatus.Failed;
                }

                if (job.Status == JobStatus.Completed)
                {
                    _logger.LogInformation("Job {Type}:{Id} has been completed.", job.Type, job.Id);
                }
                else if (job.Status == JobStatus.Failed)
                {
                    _logger.LogError("Job {Type}:{Id} has failed: {Result}", job.Type, job.Id, job.Result);
                }

                job.EndDate = DateTimeOffset.Now;
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
