using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<LogEvent> LogEvents => Set<LogEvent>();
}
