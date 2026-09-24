using System;
using System.Collections.Generic;
using System.Printing;
using PrintBot.Models;

namespace PrintBot.Services;

/// <summary>
/// Discovers printers available on the local Windows system.
/// </summary>
public class PrinterDiscoveryService
{
    /// <summary>
    /// Returns all installed print queues with their capabilities.
    /// </summary>
    public List<PrinterInfo> GetPrinters()
    {
        var printers = new List<PrinterInfo>();
        using var server = new LocalPrintServer();

        var defaultQueue = server.DefaultPrintQueue;

        foreach (var queue in server.GetPrintQueues())
        {
            var isDefault = defaultQueue != null &&
                string.Equals(queue.FullName, defaultQueue.FullName, StringComparison.OrdinalIgnoreCase);

            var paperSizes = new List<PageMediaSizeName>();
            try
            {
                var caps = queue.GetPrintCapabilities();
                if (caps.PageMediaSizeCapability != null)
                {
                    paperSizes.AddRange(caps.PageMediaSizeCapability
                        .Select(ms => ms.PageMediaSizeName ?? PageMediaSizeName.Unknown)
                        .Where(n => n != PageMediaSizeName.Unknown));
                }
            }
            catch
            {
                // Printer may be offline — return empty paper sizes
            }

            printers.Add(new PrinterInfo
            {
                Name = queue.Name,
                FullName = queue.FullName,
                Status = queue.QueueStatus,
                IsDefault = isDefault,
                IsOffline = queue.IsOffline,
                SupportedPaperSizes = paperSizes.AsReadOnly()
            });
        }

        return printers.OrderBy(p => !p.IsDefault).ThenBy(p => p.Name).ToList();
    }
}
