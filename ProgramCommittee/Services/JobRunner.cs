using Apachi.ProgramCommittee.Data;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Apachi.ProgramCommittee.Services;

public class JobRunner
{
    private readonly ILifetimeScope _container;
    private readonly ILogger _logger;

    public JobRunner(ILifetimeScope container, ILogger logger)
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
            var jobs = await appDbContext
                .Jobs.Where(job => job.Status == JobStatus.Ready || job.Status == JobStatus.Processing)
                .ToListAsync();

            foreach (var job in jobs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessJobAsync(job, lifetimeScope, appDbContext, cancellationToken);
            }
        }
    }

    private async Task ProcessJobAsync(
        Job job,
        ILifetimeScope lifetimeScope,
        AppDbContext appDbContext,
        CancellationToken cancellationToken
    )
    {
        if (job.Status == JobStatus.Processing)
        {
            job.Status = JobStatus.Failed;
            job.CompletedDate = DateTime.UtcNow;
            await appDbContext.SaveChangesAsync();
            return;
        }

        job.Status = JobStatus.Processing;
        await appDbContext.SaveChangesAsync();

        try
        {
            var processor = lifetimeScope.ResolveKeyed<IJobProcessor>(job.Type);

            if (processor == null)
            {
                job.ErrorMessage = "No job processor is defined for the job type.";
                job.Status = JobStatus.Failed;
                _logger.Error("No job processor is defined for job type {Type}.", job.Type);
                return;
            }

            await processor.ProcessJobAsync(job, cancellationToken);
            job.Status = JobStatus.Successful;
            _logger.Information("Job ({Type}:{Id}) has been completed successfully.", job.Type, job.Id);
        }
        catch (Exception exception)
        {
            job.ErrorMessage = exception.Message;
            job.Status = JobStatus.Failed;
            _logger.Error("Job ({Type}:{Id}) has failed: {ErrorMessage}", job.Type, job.Id, job.ErrorMessage);
        }
        finally
        {
            job.CompletedDate = DateTime.UtcNow;
            await appDbContext.SaveChangesAsync();
        }
    }
}
