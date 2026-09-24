using System.Collections.Generic;
using System.Printing;

namespace PrintBot.Models;

/// <summary>
/// Discovered printer information for the UI dropdown.
/// </summary>
public class PrinterInfo
{
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public PrintQueueStatus Status { get; init; }
    public bool IsDefault { get; init; }
    public bool IsOffline { get; init; }

    /// <summary>
    /// Supported paper sizes reported by this printer.
    /// </summary>
    public IReadOnlyList<PageMediaSizeName> SupportedPaperSizes { get; init; } = [];

    public override string ToString() => IsDefault ? $"{Name} (Standard)" : Name;
}
