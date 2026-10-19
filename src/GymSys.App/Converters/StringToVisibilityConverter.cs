using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GymSys.App.Converters;

/// <summary>Teks kosong/null -> Collapsed, selainnya Visible. Dipakai untuk pesan error & caption opsional.</summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
