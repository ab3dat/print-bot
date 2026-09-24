using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

    // DataGrid.SelectedItems is not a bindable DependencyProperty, so CommandParameter="{Binding
    // ElementName=JobsGrid, Path=SelectedItems}" does not refresh when the selection changes.
    // These handlers pull the current selection directly from the grid and invoke the matching
    // RelayCommand on the ViewModel instead.
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void MoveUpButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.MoveUpCommand.Execute(JobsGrid.SelectedItems.Cast<object?>().ToList());

    private void MoveDownButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.MoveDownCommand.Execute(JobsGrid.SelectedItems.Cast<object?>().ToList());

    private void RemoveSelectedButton_Click(object sender, RoutedEventArgs e) =>
        ViewModel.RemoveSelectedCommand.Execute(JobsGrid.SelectedItems.Cast<object?>().ToList());

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
