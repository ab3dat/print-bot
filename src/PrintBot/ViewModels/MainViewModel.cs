using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PrintBot.Models;
using PrintBot.Services;

namespace PrintBot.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PrintOrchestrator _orchestrator;
    private readonly PrinterDiscoveryService _printerDiscovery;
    private CancellationTokenSource? _cts;

    public MainViewModel(PrintOrchestrator orchestrator, PrinterDiscoveryService printerDiscovery)
    {
        _orchestrator = orchestrator;
        _printerDiscovery = printerDiscovery;
        _printJobs = new ObservableCollection<PrintJob>();
        _printers = new List<PrinterInfo>();
        _settings = new PrintSettings();

        LoadPrinters();
    }

    // ── Observable Properties ────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<PrintJob> _printJobs;

    [ObservableProperty]
    private List<PrinterInfo> _printers;

    [ObservableProperty]
    private PrinterInfo? _selectedPrinter;

    [ObservableProperty]
    private PrintSettings _settings;

    [ObservableProperty]
    private bool _isPrinting;

    [ObservableProperty]
    private string _statusText = "Bereit";

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private int _progressMax;

    // ── Commands ─────────────────────────────────────────────────

    [RelayCommand]
    private void AddFiles()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Dateien hinzufügen",
            Multiselect = true,
            Filter = "Unterstützte Dateien|*.pdf;*.docx;*.doc;*.xlsx;*.xls|Alle Dateien|*.*"
        };

        if (dlg.ShowDialog() != true) return;

        int added = 0;
        foreach (var path in dlg.FileNames)
        {
            if (_orchestrator.IsSupported(path) &&
                !PrintJobs.Any(j => string.Equals(j.FullPath, path, StringComparison.OrdinalIgnoreCase)))
            {
                PrintJobs.Add(new PrintJob
                {
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                    FileType = Path.GetExtension(path).TrimStart('.').ToLowerInvariant()
                });
                added++;
            }
        }

        StatusText = $"{added} Datei(en) hinzugefügt";
    }

    [RelayCommand]
    private void AddFolder()
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Ordner auswählen",
            Multiselect = false
        };

        if (dlg.ShowDialog() != true || string.IsNullOrEmpty(dlg.FolderName)) return;

        var supportedExts = _orchestrator.SupportedExtensions;
        var files = Directory.GetFiles(dlg.FolderName)
            .Where(f => supportedExts.Contains(Path.GetExtension(f).TrimStart('.')))
            .Where(f => !PrintJobs.Any(j =>
                string.Equals(j.FullPath, f, StringComparison.OrdinalIgnoreCase)));

        int added = 0;
        foreach (var path in files)
        {
            PrintJobs.Add(new PrintJob
            {
                FileName = Path.GetFileName(path),
                FullPath = path,
                FileType = Path.GetExtension(path).TrimStart('.').ToLowerInvariant()
            });
            added++;
        }

        StatusText = $"{added} Datei(en) aus Ordner hinzugefügt";
    }

    [RelayCommand]
    private void RemoveSelected(IList<object?>? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count == 0) return;

        var toRemove = selectedItems.OfType<PrintJob>().ToList();
        foreach (var job in toRemove)
            PrintJobs.Remove(job);

        StatusText = $"{toRemove.Count} Datei(en) entfernt";
    }

    [RelayCommand]
    private void ClearQueue()
    {
        if (IsPrinting) return;
        var count = PrintJobs.Count;
        if (count == 0) return;

        var result = MessageBox.Show(
            $"Wirklich alle {count} Dateien aus der Queue entfernen?",
            "Queue leeren", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            PrintJobs.Clear();
            StatusText = "Queue geleert";
        }
    }

    [RelayCommand]
    private void MoveUp(IList<object?>? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count != 1) return;
        var job = (PrintJob)selectedItems[0]!;
        var idx = PrintJobs.IndexOf(job);
        if (idx <= 0) return;

        PrintJobs.Move(idx, idx - 1);
    }

    [RelayCommand]
    private void MoveDown(IList<object?>? selectedItems)
    {
        if (selectedItems == null || selectedItems.Count != 1) return;
        var job = (PrintJob)selectedItems[0]!;
        var idx = PrintJobs.IndexOf(job);
        if (idx < 0 || idx >= PrintJobs.Count - 1) return;

        PrintJobs.Move(idx, idx + 1);
    }

    [RelayCommand]
    private async Task PrintAllAsync()
    {
        var queued = PrintJobs.Where(j => j.Status == PrintJobStatus.Queued).ToList();
        if (queued.Count == 0)
        {
            MessageBox.Show("Keine Dateien in der Queue.", "Hinweis",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show(
            $"{queued.Count} Dateien mit \"{SelectedPrinter?.Name ?? "(Standard)"}\" drucken?\n\n" +
            $"Einstellungen: Skalierung={Settings.Scaling}, " +
            $"Kopien={Settings.Copies}, " +
            $"Duplex={Settings.Duplex}",
            "Drucken bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        // Ensure printer name is set
        if (SelectedPrinter != null)
            Settings.PrinterName = SelectedPrinter.FullName;

        IsPrinting = true;
        ProgressValue = 0;
        ProgressMax = queued.Count;
        _cts = new CancellationTokenSource();

        try
        {
            var (printed, failed) = await _orchestrator.PrintQueueAsync(
                PrintJobs, Settings, OnJobStatusChanged, _cts.Token);

            StatusText = $"Fertig: {printed} gedruckt, {failed} fehlgeschlagen";

            if (failed > 0)
            {
                MessageBox.Show(
                    $"{printed} Datei(en) erfolgreich gedruckt.\n" +
                    $"{failed} Datei(en) fehlgeschlagen — siehe rote Markierung in der Queue.",
                    "Druck abgeschlossen", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (OperationCanceledException)
        {
            StatusText = "Druck abgebrochen";
        }
        finally
        {
            IsPrinting = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cts?.Cancel();
        StatusText = "Breche ab...";
    }

    // ── Helpers ──────────────────────────────────────────────────

    private void LoadPrinters()
    {
        try
        {
            Printers = _printerDiscovery.GetPrinters();
            SelectedPrinter = Printers.FirstOrDefault(p => p.IsDefault) ?? Printers.FirstOrDefault();
        }
        catch (Exception ex)
        {
            StatusText = $"Fehler beim Laden der Drucker: {ex.Message}";
        }
    }

    private void OnJobStatusChanged(PrintJob job, int index)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Trigger UI refresh: remove and re-insert at same position
            var idx = PrintJobs.IndexOf(job);
            if (idx >= 0)
            {
                PrintJobs.RemoveAt(idx);
                PrintJobs.Insert(idx, job);
            }

            ProgressValue = PrintJobs.Count(j =>
                j.Status is PrintJobStatus.Printed or PrintJobStatus.Failed or PrintJobStatus.Skipped);

            StatusText = $"Drucke... {ProgressValue}/{ProgressMax}";
        });
    }
}
