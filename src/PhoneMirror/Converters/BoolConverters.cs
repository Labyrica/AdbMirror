using Avalonia.Data.Converters;

namespace PhoneMirror.Converters;

/// <summary>
/// Static bool converters for XAML bindings.
/// </summary>
public static class BoolConverters
{
    /// <summary>
    /// Converts a boolean to a chevron symbol (expanded = down, collapsed = right).
    /// </summary>
    public static readonly IValueConverter ToChevron =
        new FuncValueConverter<bool, string>(isExpanded =>
            isExpanded ? "\uE70D" : "\uE70E");
}
