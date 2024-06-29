using Apachi.WebApi.Data;

namespace Apachi.WebApi.Services;

public class JobScheduler
{
    private readonly AppDbContext _dbContext;

    public JobScheduler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task ScheduleJobAsync(JobType type, string? payload)
    {
        var job = new Job
        {
            Type = type,
            Payload = payload,
            ScheduleDate = DateTimeOffset.Now
        };
        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync();
    }
}
