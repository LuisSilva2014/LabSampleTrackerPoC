using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LabSampleTracker.Desktop.Converters;

/// <summary>
/// Paints the status pill. Pass ConverterParameter "Foreground" for the text color.
/// </summary>
public class StatusBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isPending = string.Equals(value as string, "Pending", StringComparison.OrdinalIgnoreCase);
        var wantForeground = string.Equals(parameter as string, "Foreground", StringComparison.OrdinalIgnoreCase);

        if (wantForeground)
        {
            return isPending
                ? Freeze(Color.FromRgb(140, 90, 20))
                : Freeze(Color.FromRgb(14, 90, 82));
        }

        return isPending
            ? Freeze(Color.FromRgb(255, 236, 204))
            : Freeze(Color.FromRgb(214, 239, 234));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
