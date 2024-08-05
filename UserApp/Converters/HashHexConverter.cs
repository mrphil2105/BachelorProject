using System.Globalization;
using Avalonia.Data.Converters;

namespace Apachi.UserApp.Converters;

public class HashHexConverter : IValueConverter
{
    private readonly int _hexLength;

    public HashHexConverter(int hexLength)
    {
        _hexLength = hexLength;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] hash)
        {
            var hashHex = System.Convert.ToHexString(hash);
            var shortHex = hashHex.Remove(_hexLength);
            return shortHex;
        }

        throw new ArgumentException("Value must be a byte array.");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
