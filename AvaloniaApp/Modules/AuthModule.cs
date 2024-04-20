using Apachi.AvaloniaApp.Auth;
using Apachi.AvaloniaApp.Data;
using Apachi.ViewModels.Auth;
using Autofac;

namespace Apachi.AvaloniaApp.Modules;

public class AuthModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .Register(context =>
            {
                var dbContextFactory = context.Resolve<Func<AppDbContext>>();
                return new Session(dbContextFactory);
            })
            .As<ISession>()
            .SingleInstance();
    }
}
