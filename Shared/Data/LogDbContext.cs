using Microsoft.EntityFrameworkCore;

namespace Apachi.Shared.Data;

public class LogDbContext : DbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options)
        : base(options) { }

    public DbSet<Submitter> Submitters => Set<Submitter>();

    public DbSet<Reviewer> Reviewers => Set<Reviewer>();

    public DbSet<LogEntry> Entries => Set<LogEntry>();
}
