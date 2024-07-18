using Apachi.ProgramCommittee.Data;

namespace Apachi.ProgramCommittee.Services;

public interface IJobProcessor
{
    Task<string?> ProcessJobAsync(Job job, CancellationToken cancellationToken);
}
