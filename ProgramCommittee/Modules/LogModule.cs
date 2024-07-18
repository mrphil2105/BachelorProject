using Autofac;
using Serilog;

namespace Apachi.ProgramCommittee.Modules;

public class LogModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .Register<ILogger>(context =>
            {
                var logger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("Log.txt").CreateLogger();
                return logger;
            })
            .SingleInstance();
    }
}
