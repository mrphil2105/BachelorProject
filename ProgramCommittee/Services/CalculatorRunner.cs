using Apachi.ProgramCommittee.Calculators;
using Autofac;
using Serilog;

namespace Apachi.ProgramCommittee.Services;

public class CalculatorRunner
{
    private readonly ILifetimeScope _container;
    private readonly ILogger _logger;

    public CalculatorRunner(ILifetimeScope container, ILogger logger)
    {
        _container = container;
        _logger = logger;
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
                    _logger.Error(
                        "Calculator {Type} has failed: {Message}",
                        calculator.GetType().Name,
                        exception.Message
                    );
                }
            }
        }
    }
}
