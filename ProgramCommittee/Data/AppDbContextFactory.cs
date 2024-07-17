using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Apachi.ProgramCommittee.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var sqliteFileName = Environment.GetEnvironmentVariable("APP_DATABASE_FILE");

        if (sqliteFileName == null)
        {
            throw new Exception($"Environment variable APP_DATABASE_FILE must be set.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite($"Data Source={sqliteFileName}");
        return new AppDbContext(optionsBuilder.Options);
    }
}
