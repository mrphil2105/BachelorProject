using Microsoft.EntityFrameworkCore;

namespace Apachi.AvaloniaApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Submitter> Submitters => Set<Submitter>();

    public DbSet<Reviewer> Reviewers => Set<Reviewer>();

    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Submitter>().HasIndex(user => user.Username).IsUnique();
        builder.Entity<Reviewer>().HasIndex(user => user.Username).IsUnique();
    }
}
