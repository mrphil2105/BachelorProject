namespace Apachi.ProgramCommittee.Calculators;

public interface ICalculator
{
    Task CalculateAsync(CancellationToken cancellationToken);
}
