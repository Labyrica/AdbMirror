using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PhoneMirror.Converters;

/// <summary>
/// Converts log level strings to appropriate brush colors.
/// </summary>
public class LogLevelToBrushConverter : IValueConverter
{
    /// <summary>
    /// Brush used for Error level logs.
    /// </summary>
    public static readonly SolidColorBrush ErrorBrush = new(Color.FromRgb(0xFF, 0x44, 0x44));

    /// <summary>
    /// Brush used for Warning level logs.
    /// </summary>
    public static readonly SolidColorBrush WarningBrush = new(Color.FromRgb(0xFF, 0xAA, 0x00));

    /// <summary>
    /// Brush used for Info level logs.
    /// </summary>
    public static readonly SolidColorBrush InfoBrush = new(Color.FromRgb(0x88, 0x88, 0x88));

    /// <summary>
    /// Brush used for Debug level logs.
    /// </summary>
    public static readonly SolidColorBrush DebugBrush = new(Color.FromRgb(0x66, 0x66, 0x66));

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string level)
        {
            return level.ToUpperInvariant() switch
            {
                "E" => ErrorBrush,
                "W" => WarningBrush,
                "I" => InfoBrush,
                "D" => DebugBrush,
                "V" => DebugBrush,
                "F" => ErrorBrush, // Fatal
                _ => InfoBrush
            };
        }

        return InfoBrush;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
