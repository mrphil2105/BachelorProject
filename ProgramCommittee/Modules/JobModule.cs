using System.Reflection;
using Apachi.ProgramCommittee.Data;
using Apachi.ProgramCommittee.Services;
using Autofac;
using Module = Autofac.Module;

namespace Apachi.ProgramCommittee.Modules;

public class JobModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<JobScheduler>();
        builder.RegisterType<JobRunner>();

        var processorTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo<IJobProcessor>())
            .ToList();

        foreach (var jobType in Enum.GetValues<JobType>())
        {
            var processorName = jobType + "Processor";
            var processorType = processorTypes.FirstOrDefault(type => type.Name == processorName);

            if (processorType == null)
            {
                continue;
            }

            builder.RegisterType(processorType).Keyed<IJobProcessor>(jobType);
        }
    }
}
