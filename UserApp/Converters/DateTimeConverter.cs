using System.Globalization;
using Avalonia.Data.Converters;

namespace Apachi.UserApp.Converters;

public class DateTimeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var humanReadable = dateTime.ToString("dd/MM/yyyy HH:mm");
            return humanReadable;
        }

        throw new ArgumentException($"Value must be a {nameof(DateTime)}.");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
