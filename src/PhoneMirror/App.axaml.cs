using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PhoneMirror.Core.Execution;
using PhoneMirror.Core.Platform;
using PhoneMirror.Core.Services;
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
            // Initialize resource extraction (fire-and-forget for startup speed)
            var resourceExtractor = Services.GetRequiredService<IResourceExtractor>();
            _ = resourceExtractor.ExtractAllAsync();

            // Get MainWindowViewModel from DI container
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel
            };

            // Register cleanup on app exit
            desktop.ShutdownRequested += (_, _) => resourceExtractor.Cleanup();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Configures services for dependency injection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        // Register platform services
        services.AddSingleton<IPlatformService, PlatformService>();
        services.AddSingleton<ProcessRunner>();

        // Register resource extractor
        services.AddSingleton<IResourceExtractor, ResourceExtractor>();

        // Register ViewModels
        services.AddSingleton<MainWindowViewModel>();

        // Register core services
        services.AddSingleton<IAdbService, AdbService>();
        services.AddSingleton<IScrcpyService, ScrcpyService>();
    }
}
