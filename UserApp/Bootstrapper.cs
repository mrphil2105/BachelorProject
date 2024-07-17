using System.Reflection;
using Apachi.UserApp.Data;
using Apachi.ViewModels;
using Autofac;
using Microsoft.EntityFrameworkCore;
using MvvmElegance;

namespace Apachi.UserApp;

public class Bootstrapper : AutofacBootstrapper<MainViewModel>
{
    protected override void ConfigureServices(ContainerBuilder builder)
    {
        builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
    }

    protected override void Configure()
    {
        using var dbContext = GetService<AppDbContext>();
        dbContext.Database.Migrate();
    }
}
