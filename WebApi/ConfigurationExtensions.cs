namespace Apachi.WebApi;

public static class ConfigurationExtensions
{
    public static string GetSubmissionsStorage(this IConfiguration configuration)
    {
        var submissionsDirectoryPath = configuration.GetSection("Storage").GetValue<string>("Submissions");

        if (submissionsDirectoryPath == null)
        {
            throw new InvalidOperationException(
                "A Submissions storage directory must be specified in application settings."
            );
        }

        return submissionsDirectoryPath;
    }

    public static string GetReviewsStorage(this IConfiguration configuration)
    {
        var reviewsDirectoryPath = configuration.GetSection("Storage").GetValue<string>("Reviews");

        if (reviewsDirectoryPath == null)
        {
            throw new InvalidOperationException(
                "A Reviews storage directory must be specified in application settings."
            );
        }

        return reviewsDirectoryPath;
    }
}
