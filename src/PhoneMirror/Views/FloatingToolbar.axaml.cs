using Avalonia.Controls;

namespace PhoneMirror.Views;

/// <summary>
/// Floating toolbar window that appears during mirroring sessions.
/// Provides quick access to screenshot capture and error log copying.
/// </summary>
public partial class FloatingToolbar : Window
{
    /// <summary>
    /// Initializes a new instance of the FloatingToolbar.
    /// </summary>
    public FloatingToolbar()
    {
        InitializeComponent();
    }
}
