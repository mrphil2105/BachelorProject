using Apachi.Shared.Data;

namespace Apachi.Shared.Factories;

public partial class MessageFactory : IDisposable, IAsyncDisposable
{
    private readonly LogDbContext _logDbContext;

    public MessageFactory(LogDbContext logDbContext)
    {
        _logDbContext = logDbContext;
    }

    public void Dispose()
    {
        _logDbContext.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _logDbContext.DisposeAsync();
    }
}
