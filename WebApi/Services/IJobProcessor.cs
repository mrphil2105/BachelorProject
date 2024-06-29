using Apachi.WebApi.Data;

namespace Apachi.WebApi.Services;

public interface IJobProcessor
{
    Task<string?> ProcessJobAsync(Job job, CancellationToken cancellationToken);
}
