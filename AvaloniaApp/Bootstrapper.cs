using System.Reflection;
using Apachi.ViewModels;
using Autofac;
using MvvmElegance;

namespace Apachi.AvaloniaApp;

public class Bootstrapper : AutofacBootstrapper<MainViewModel>
{
    protected override void ConfigureServices(ContainerBuilder builder)
    {
        builder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
    }
}
