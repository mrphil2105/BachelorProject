using System.Reflection;
using Apachi.ViewModels;
using Autofac;
using Module = Autofac.Module;

namespace Apachi.UserApp.Modules;

public class ViewModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterAssemblyTypes(typeof(MainViewModel).Assembly)
            .Where(type => type.Name.EndsWith("ViewModel"))
            .AsSelf()
            .AsImplementedInterfaces();
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(type => type.Name.EndsWith("View"));
    }
}
