using Apachi.ViewModels;
using Autofac;

namespace Apachi.UserApp.Modules;

public class ValidationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(MainViewModel).Assembly)
            .Where(t => t.Namespace?.EndsWith("Validation") ?? false);
    }
}
