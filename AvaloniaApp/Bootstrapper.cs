using System.Reflection;
using Apachi.ViewModels;
using Autofac;
using MvvmElegance;

namespace Apachi.AvaloniaApp;

public class Bootstrapper : AutofacBootstrapper<MainViewModel>
{
    protected override void ConfigureServices(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(MainViewModel).Assembly).Where(t => t.Name.EndsWith("ViewModel"));
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(t => t.Name.EndsWith("View"));
    }
}
