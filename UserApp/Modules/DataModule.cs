using Apachi.Shared;
using Apachi.Shared.Data;
using Apachi.UserApp.Data;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Apachi.UserApp.Modules;

public class DataModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var sqliteFileName = EnvironmentVariable.GetValue(EnvironmentVariable.AppDatabaseFile);
        var databaseConnection = EnvironmentVariable.GetValue(EnvironmentVariable.LogDatabaseConnection);

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
