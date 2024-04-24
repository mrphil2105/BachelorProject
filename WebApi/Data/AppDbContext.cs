using Microsoft.EntityFrameworkCore;

namespace Apachi.WebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<Submission> Submissions => Set<Submission>();
}
