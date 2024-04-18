using Apachi.Shared.Crypt;
using Autofac;

namespace Apachi.AvaloniaApp.Modules;

public class ServicesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(context => PublicKeyStore.FromFile()).SingleInstance();
    }
}
