using System.Globalization;
using Avalonia.Data.Converters;

namespace Apachi.UserApp.Converters;

public class DateTimeOffsetConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTimeOffset dateTime)
        {
            var humanReadable = dateTime.ToString("dd/MM/yyyy HH:mm");
            return humanReadable;
        }

        throw new ArgumentException($"Value must be a {nameof(DateTimeOffset)}.");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
