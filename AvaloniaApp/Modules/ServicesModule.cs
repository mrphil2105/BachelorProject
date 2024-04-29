using System.Reflection;
using Apachi.AvaloniaApp.Services;
using Apachi.ViewModels.Services;
using Autofac;
using Module = Autofac.Module;

namespace Apachi.AvaloniaApp.Modules;

public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(t => t.IsInNamespaceOf<SubmissionService>())
            .AsImplementedInterfaces();
        builder.RegisterType<SessionService>().As<ISessionService>().SingleInstance();
    }
}
