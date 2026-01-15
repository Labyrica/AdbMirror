using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PhoneMirror.ViewModels;
using PhoneMirror.Views;

namespace PhoneMirror;

public partial class App : Application
{
    /// <summary>
    /// The application's service provider for dependency injection.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Get MainWindowViewModel from DI container
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Future: Register services from PhoneMirror.Core here
        // services.AddSingleton<IAdbService, AdbService>();
        // services.AddSingleton<IScrcpyService, ScrcpyService>();
    }
}
