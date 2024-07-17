using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}
