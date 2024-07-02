using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<Reviewer> Reviewers => Set<Reviewer>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<JobSchedule> JobSchedules => Set<JobSchedule>();

    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Submission>().Property(submission => submission.Status).HasConversion<string>();

        builder.Entity<Review>().Property(review => review.Status).HasConversion<string>();

        builder.Entity<JobSchedule>().Property(schedule => schedule.JobType).HasConversion<string>();
        builder.Entity<JobSchedule>().HasIndex(schedule => schedule.JobType).IsUnique();
        builder.Entity<JobSchedule>().Property(schedule => schedule.Status).HasConversion<string>();

        builder.Entity<Job>().Property(job => job.Type).HasConversion<string>();
        builder.Entity<Job>().Property(job => job.Status).HasConversion<string>();
    }
}
