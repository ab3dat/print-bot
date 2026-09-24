using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PrintBot.Models;
using Serilog;

namespace PrintBot.Services;

/// <summary>
/// Orchestrates the print queue: dispatches jobs to the correct
/// file-type service and tracks progress.
/// </summary>
public class PrintOrchestrator
{
    private readonly Dictionary<string, IPrintService> _services;

    public PrintOrchestrator(PdfPrintService pdfService, OfficePrintService officeService)
    {
        _services = new Dictionary<string, IPrintService>(StringComparer.OrdinalIgnoreCase);
        Register(pdfService);
        Register(officeService);
    }

    private void Register(IPrintService service)
    {
        foreach (var ext in service.SupportedExtensions)
            _services[ext] = service;
    }

    /// <summary>
    /// Returns the set of all supported file extensions.
    /// </summary>
    public IReadOnlySet<string> SupportedExtensions =>
        new HashSet<string>(_services.Keys, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks whether a file extension is supported.
    /// </summary>
    public bool IsSupported(string filePath)
    {
        var ext = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
        return _services.ContainsKey(ext);
    }

    /// <summary>
    /// Print all queued jobs sequentially, reporting progress.
    /// Returns counts of printed and failed jobs.
    /// </summary>
    public async Task<(int Printed, int Failed)> PrintQueueAsync(
        IReadOnlyList<PrintJob> jobs,
        PrintSettings settings,
        Action<PrintJob, int> onStatusChanged,
        CancellationToken ct)
    {
        int printed = 0, failed = 0;

        for (int i = 0; i < jobs.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var job = jobs[i];
            if (job.Status is PrintJobStatus.Printed or PrintJobStatus.Skipped)
                continue;

            job.Status = PrintJobStatus.Printing;
            job.ErrorMessage = null;
            onStatusChanged(job, i);

            try
            {
                var ext = Path.GetExtension(job.FullPath).TrimStart('.').ToLowerInvariant();

                if (!_services.TryGetValue(ext, out var service))
                {
                    job.Status = PrintJobStatus.Failed;
                    job.ErrorMessage = $"Unsupported file type: .{ext}";
                    onStatusChanged(job, i);
                    failed++;
                    Log.Warning("Unsupported file type: {Path}", job.FullPath);
                    continue;
                }

                bool success = await service.PrintAsync(job, settings, ct);
                if (success)
                {
                    job.Status = PrintJobStatus.Printed;
                    job.PrintedAt = DateTime.Now;
                    onStatusChanged(job, i);
                    printed++;
                    Log.Information("Printed: {Path}", job.FullPath);
                }
                else
                {
                    job.Status = PrintJobStatus.Failed;
                    job.ErrorMessage = "Print service returned false";
                    onStatusChanged(job, i);
                    failed++;
                    Log.Warning("Print failed (false): {Path}", job.FullPath);
                }
            }
            catch (OperationCanceledException)
            {
                // Mark remaining queued jobs as skipped
                job.Status = PrintJobStatus.Skipped;
                onStatusChanged(job, i);
                for (int j = i + 1; j < jobs.Count; j++)
                {
                    if (jobs[j].Status == PrintJobStatus.Queued)
                    {
                        jobs[j].Status = PrintJobStatus.Skipped;
                        onStatusChanged(jobs[j], j);
                    }
                }
                throw; // rethrow to signal cancellation
            }
            catch (Exception ex)
            {
                job.Status = PrintJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                onStatusChanged(job, i);
                failed++;
                Log.Error(ex, "Print failed: {Path}", job.FullPath);
            }

            // Delay between jobs to avoid spooler overload
            if (i < jobs.Count - 1 && settings.DelayBetweenJobsMs > 0)
            {
                try { await Task.Delay(settings.DelayBetweenJobsMs, ct); }
                catch (OperationCanceledException) { /* handled above */ }
            }
        }

        return (printed, failed);
    }
}
