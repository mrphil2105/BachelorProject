using Apachi.ProgramCommittee.Calculators;
using Autofac;

namespace Apachi.ProgramCommittee.Services;

public class CalculatorRunner
{
    private readonly ILifetimeScope _container;

    public CalculatorRunner(ILifetimeScope container)
    {
        _container = container;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var period = TimeSpan.FromSeconds(10);
        using var timer = new PeriodicTimer(period);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await using var lifetimeScope = _container.BeginLifetimeScope();
            var calculators = lifetimeScope.Resolve<IEnumerable<ICalculator>>();

            foreach (var calculator in calculators)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await calculator.CalculateAsync(cancellationToken);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Calculator {0} has failed: {1}", calculator.GetType().Name, exception.Message);
                }
            }
        }
    }
}
