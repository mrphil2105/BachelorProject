using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Apachi.Shared.Data;

public class LogDbContextFactory : IDesignTimeDbContextFactory<LogDbContext>
{
    public LogDbContext CreateDbContext(string[] args)
    {
        var databaseConnection = EnvironmentVariable.GetValue(EnvironmentVariable.LogDatabaseConnection);
        var optionsBuilder = new DbContextOptionsBuilder<LogDbContext>();
        optionsBuilder.UseNpgsql(databaseConnection);
        return new LogDbContext(optionsBuilder.Options);
    }
}
