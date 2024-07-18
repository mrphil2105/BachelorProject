using Apachi.ProgramCommittee.Services;
using Autofac;

namespace Apachi.ProgramCommittee.Modules;

public class JobModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<JobScheduler>();
        builder.RegisterType<JobRunner>();
    }
}
