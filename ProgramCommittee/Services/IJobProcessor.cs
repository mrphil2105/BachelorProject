using Apachi.ProgramCommittee.Data;

namespace Apachi.ProgramCommittee.Services;

public interface IJobProcessor
{
    Task ProcessJobAsync(Job job, CancellationToken cancellationToken);
}
