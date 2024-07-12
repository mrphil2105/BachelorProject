namespace Apachi.WebApi.Data;

public class AppendOnlyLog
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public AppendOnlyLog(
        IConfiguration configuration,
        AppDbContext dbContext
    )
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }
    
    
}