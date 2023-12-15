using System.Globalization;

namespace MauiFeed.MauiUI.Converters;

public class ImageByteConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes)
        {
            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}