using System.Reflection;
using Apachi.ProgramCommittee.Calculators;
using Apachi.ProgramCommittee.Services;
using Autofac;
using Module = Autofac.Module;

namespace Apachi.ProgramCommittee.Modules;

public class CalculatorModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<CalculatorRunner>();
        builder
            .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Where(type => type.IsAssignableTo<ICalculator>())
            .AsImplementedInterfaces();
    }
}
