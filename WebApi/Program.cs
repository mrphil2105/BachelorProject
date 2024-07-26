using Apachi.WebApi.Data;
using Apachi.WebApi.Services;
using Microsoft.EntityFrameworkCore;

if (args.Length > 0 && args[0] == "--generate-keypair")
{
    var (publicKey, privateKey) = await GenerateKeyPairAsync();
    var publicKeyBase64 = Convert.ToBase64String(publicKey);
    var privateKeyBase64 = Convert.ToBase64String(privateKey);
    Console.WriteLine("Public Key: {0}", publicKeyBase64);
    Console.WriteLine("Private Key: {0}", privateKeyBase64);
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=App.db"));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<JobScheduler>();
builder.Services.AddHostedService<JobRunner>();

builder.Services.AddKeyedTransient<IJobProcessor, CreateReviewsJobProcessor>(JobType.CreateReviews);
builder.Services.AddKeyedTransient<IJobProcessor, MatchingJobProcessor>(JobType.Matching);
builder.Services.AddKeyedTransient<IJobProcessor, ShareAssessmentsJobProcessor>(JobType.ShareAssessments);

builder.Configuration.AddEnvironmentVariables("APACHI_");

var app = builder.Build();

using (var serviceScope = app.Services.CreateScope())
using (var dbContext = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>())
{
    dbContext.Database.Migrate();

    foreach (var jobType in Enum.GetValues<JobType>())
    {
        EnsureJobSchedule(jobType, dbContext);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

void EnsureJobSchedule(JobType jobType, AppDbContext dbContext)
{
    var scheduleExists = dbContext.JobSchedules.Any(schedule => schedule.JobType == jobType);

    if (!scheduleExists)
    {
        var jobSchedule = new JobSchedule { JobType = jobType, Interval = TimeSpan.FromMinutes(5) };
        dbContext.JobSchedules.Add(jobSchedule);
        dbContext.SaveChanges();
    }
}
