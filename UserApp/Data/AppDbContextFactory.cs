using Apachi.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Apachi.UserApp.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var sqliteFileName = EnvironmentVariable.GetValue(EnvironmentVariable.AppDatabaseFile);
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite($"Data Source={sqliteFileName}");
        return new AppDbContext(optionsBuilder.Options);
    }
}
