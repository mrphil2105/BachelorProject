using Apachi.Shared.Factories;
using Autofac;

namespace Apachi.ProgramCommittee.Modules;

public class FactoriesModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<MessageFactory>();
    }
}
