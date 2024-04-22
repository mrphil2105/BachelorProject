using Apachi.AvaloniaApp.Data;
using Autofac;
using Microsoft.EntityFrameworkCore;

namespace Apachi.AvaloniaApp.Modules
{
    public class DataModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .Register(context =>
                {
                    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                    optionsBuilder.UseSqlite("Data Source=App.db");
                    return optionsBuilder.Options;
                })
                .SingleInstance();
            builder.RegisterType<AppDbContext>();
        }
    }
}
