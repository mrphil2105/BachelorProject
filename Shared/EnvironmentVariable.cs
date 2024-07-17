namespace Apachi.Shared;

public static class EnvironmentVariable
{
    public const string LogDatabaseConnection = "APACHI_LOG_DATABASE_CONNECTION";
    public const string AppDatabaseFile = "APACHI_APP_DATABASE_FILE";

    public static string GetValue(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (value == null)
        {
            throw new Exception($"Environment variable {name} must be set.");
        }

        return value;
    }
}
