using System.Reflection;
using Apachi.ProgramCommittee.Data;
using Apachi.ProgramCommittee.Services;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee;

public class Program
{
    private static readonly CancellationTokenSource _cancellationSource = new();

    public static async Task Main(string[] args)
    {
        Console.CancelKeyPress += OnCancelKeyPress;

        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
        var container = containerBuilder.Build();

        await PrepareAsync(container);

        var jobScheduler = container.Resolve<JobScheduler>();
        var jobRunner = container.Resolve<JobRunner>();

        try
        {
            var jobSchedulerTask = jobScheduler.ExecuteAsync(_cancellationSource.Token);
            var jobRunnerTask = jobRunner.ExecuteAsync(_cancellationSource.Token);
            await Task.WhenAll(jobSchedulerTask, jobRunnerTask);
        }
        catch (OperationCanceledException) { }
    }

    private static async Task PrepareAsync(ILifetimeScope container)
    {
        using (var lifetimeScope = container.BeginLifetimeScope())
        using (var dbContext = lifetimeScope.Resolve<AppDbContext>())
        {
            await dbContext.Database.MigrateAsync();

            foreach (var jobType in Enum.GetValues<JobType>())
            {
                await EnsureJobScheduleAsync(jobType, dbContext);
            }
        }
    }

    private static async Task EnsureJobScheduleAsync(JobType jobType, AppDbContext dbContext)
    {
        var scheduleExists = await dbContext.JobSchedules.AnyAsync(schedule => schedule.JobType == jobType);

        if (!scheduleExists)
        {
            var jobSchedule = new JobSchedule { JobType = jobType, Interval = TimeSpan.FromSeconds(30) };
            dbContext.JobSchedules.Add(jobSchedule);
            await dbContext.SaveChangesAsync();
        }
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        if (_cancellationSource.IsCancellationRequested)
        {
            return;
        }

        Console.WriteLine("Exiting gracefully... Press Ctrl+C again to exit forcefully.");
        e.Cancel = true;
        _cancellationSource.Cancel();
    }
}
