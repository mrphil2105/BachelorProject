using Apachi.ProgramCommittee.Data;
using Apachi.Shared.Data;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Apachi.ProgramCommittee.Modules;

public class DataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var sqliteFileName = Environment.GetEnvironmentVariable("APP_DATABASE_FILE");
        var databaseConnection = Environment.GetEnvironmentVariable("LOG_DATABASE_CONNECTION");

        if (sqliteFileName == null || databaseConnection == null)
        {
            throw new Exception($"Environment variables APP_DATABASE_FILE and LOG_DATABASE_CONNECTION must be set.");
        }

        builder
            .Register(context =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseSqlite($"Data Source={sqliteFileName}");
                return optionsBuilder.Options;
            })
            .SingleInstance();
        builder.RegisterType<AppDbContext>();

        builder
            .Register(context =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<LogDbContext>();
                optionsBuilder.UseNpgsql(databaseConnection);
                return optionsBuilder.Options;
            })
            .SingleInstance();
        builder.RegisterType<LogDbContext>();
    }
}
