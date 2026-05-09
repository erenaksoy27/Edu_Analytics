using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EduAnalytics.UI.Converters;

/// <summary>
/// int/collection count → Visibility.
/// 0 → Visible (empty state göster), >0 → Collapsed.
/// </summary>
public class CollectionEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var count = value switch
        {
            int i    => i,
            IEnumerable<object> e => e.Count(),
            System.Collections.IEnumerable e => e.Cast<object>().Count(),
            _ => -1
        };
        return count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
