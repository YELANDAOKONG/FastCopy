// App.xaml.cs
namespace FastCopy;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using FastCopy.Services;
using FastCopy.ViewModels;
using FastCopy.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            DisableAvaloniaDataAnnotationValidation();
            
            // Create main window
            var window = new MainWindow();
            
            // Set as main window before setting DataContext so clipboard service can access it
            desktop.MainWindow = window;
            
            // Get view model from service provider
            var mainViewModel = _serviceProvider!.GetRequiredService<MainWindowViewModel>();
            window.DataContext = mainViewModel;
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void ConfigureServices(ServiceCollection services)
    {
        // Register services
        services.AddSingleton<IClipboardService, ClipboardService>();
        
        // Register view models
        services.AddTransient<MainWindowViewModel>();
    }
    
    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
