using System.Globalization;
using Avalonia.Data.Converters;

namespace Apachi.UserApp.Converters;

public class GradeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int grade)
        {
            var gradeString = grade switch
            {
                0 => "02",
                2 => "02",
                _ => grade.ToString()
            };
            return gradeString;
        }

        throw new ArgumentException("Value must be an integer.", nameof(value));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
