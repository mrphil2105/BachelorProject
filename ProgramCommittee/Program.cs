using System.Reflection;
using Autofac;

namespace Apachi.ProgramCommittee;

public class Program
{
    public static void Main(string[] args)
    {
        var containerBuilder = new ContainerBuilder();
        containerBuilder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
        var container = containerBuilder.Build();
    }
}
