using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PrintBot.Models;

namespace PrintBot.Services;

/// <summary>
/// Abstraction over file-type-specific print backends.
/// </summary>
public interface IPrintService
{
    /// <summary>
    /// Supported file extensions (lowercase, e.g. "pdf", "docx").
    /// </summary>
    IReadOnlySet<string> SupportedExtensions { get; }

    /// <summary>
    /// Print a single file with the given settings.
    /// Returns true on success, throws on unrecoverable error.
    /// </summary>
    Task<bool> PrintAsync(PrintJob job, PrintSettings settings, CancellationToken ct);
}
