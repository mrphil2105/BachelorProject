using Apachi.WebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Services;

public class JobRunner : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobRunner> _logger;

    public JobRunner(IServiceScopeFactory scopeFactory, ILogger<JobRunner> logger)
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
            var jobs = await dbContext
                .Jobs.Where(job => job.Status == JobStatus.Ready || job.Status == JobStatus.Processing)
                .ToListAsync();

            foreach (var job in jobs)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await ProcessJobAsync(job, scope.ServiceProvider, dbContext, stoppingToken);
            }
        }
    }

    private async Task ProcessJobAsync(
        Job job,
        IServiceProvider serviceProvider,
        AppDbContext dbContext,
        CancellationToken stoppingToken
    )
    {
        if (job.Status == JobStatus.Processing)
        {
            job.Status = JobStatus.Failed;
            job.CompletedDate = DateTimeOffset.Now;
            await dbContext.SaveChangesAsync();
            return;
        }

        job.Status = JobStatus.Processing;
        await dbContext.SaveChangesAsync();

        try
        {
            var processor = serviceProvider.GetKeyedService<IJobProcessor>(job.Type);

            if (processor == null)
            {
                job.Result = "No job processor is defined for the job type.";
                job.Status = JobStatus.Failed;
                _logger.LogError("No job processor is defined for job type {Type}.", job.Type);
            }
            else
            {
                job.Result = await processor.ProcessJobAsync(job, stoppingToken);
                job.Status = JobStatus.Successful;
                _logger.LogInformation("Job ({Type}:{Id}) has been completed successfully.", job.Type, job.Id);
            }
        }
        catch (Exception exception)
        {
            job.Result = exception.Message;
            job.Status = JobStatus.Failed;
            _logger.LogError("Job ({Type}:{Id}) has failed: {Result}", job.Type, job.Id, job.Result);
        }

        job.CompletedDate = DateTimeOffset.Now;
        await dbContext.SaveChangesAsync();
    }
}
