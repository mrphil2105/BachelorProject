using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Submitter> Submitters => Set<Submitter>();

    public DbSet<Reviewer> Reviewers => Set<Reviewer>();

    public DbSet<Submission> Submissions => Set<Submission>();

    public DbSet<Review> Reviews => Set<Review>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Submitter>().HasIndex(submitter => submitter.Username).IsUnique();
        builder.Entity<Reviewer>().HasIndex(reviewer => reviewer.Username).IsUnique();
    }
}
