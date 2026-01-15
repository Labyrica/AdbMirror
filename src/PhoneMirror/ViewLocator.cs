using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PhoneMirror.ViewModels;

namespace PhoneMirror;

/// <summary>
/// ViewLocator that resolves Views for ViewModels by convention.
/// FooViewModel in ViewModels namespace -> FooView in Views namespace.
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// Determines if this data template matches the provided data.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data is a ViewModelBase type.</returns>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }

    /// <summary>
    /// Builds a view for the given data (ViewModel).
    /// </summary>
    /// <param name="data">The ViewModel to build a view for.</param>
    /// <returns>The corresponding View, or a TextBlock with error message if not found.</returns>
    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "Data is null" };
        }

        var viewModelType = data.GetType();
        var viewModelName = viewModelType.FullName;

        if (string.IsNullOrEmpty(viewModelName))
        {
            return new TextBlock { Text = "ViewModel type name is null" };
        }

        // Convention: PhoneMirror.ViewModels.FooViewModel -> PhoneMirror.Views.FooView
        var viewName = viewModelName
            .Replace(".ViewModels.", ".Views.")
            .Replace("ViewModel", "View");

        var viewType = Type.GetType(viewName);

        if (viewType != null)
        {
            var view = Activator.CreateInstance(viewType);
            if (view is Control control)
            {
                return control;
            }
        }

        return new TextBlock { Text = $"Not Found: {viewName}" };
    }
}
