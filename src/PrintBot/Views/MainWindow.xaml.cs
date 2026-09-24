using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrintBot.Services;
using PrintBot.ViewModels;
using Serilog;

namespace PrintBot.Views;

public partial class MainWindow : Window
{
    private readonly ServiceProvider _serviceProvider;

    public MainWindow()
    {
        _serviceProvider = ConfigureServices();
        DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        InitializeComponent();
    }

    private static ServiceProvider ConfigureServices()
    {
        // Configure Serilog
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PrintBot", "printbot.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();

        // Services
        services.AddSingleton<PrinterDiscoveryService>();
        services.AddSingleton<PdfPrintService>();
        services.AddSingleton<OfficePrintService>();
        services.AddSingleton<PrintOrchestrator>();

        // ViewModels
        services.AddTransient<MainViewModel>();

        return services.BuildServiceProvider();
    }

    protected override void OnClosed(EventArgs e)
    {
        _serviceProvider.Dispose();
        Log.CloseAndFlush();
        base.OnClosed(e);
    }
}
