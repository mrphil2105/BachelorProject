namespace Apachi.UserApp.Converters;

public static class CustomConverters
{
    public static readonly DateTimeConverter DateTime = new();

    public static readonly HashHexConverter ShortHashHex = new(10);

    public static readonly HashHexConverter LongHashHex = new(16);

    public static readonly GradeConverter Grade = new();
}
