using System.Printing;

namespace PrintBot.Models;

/// <summary>
/// How a PDF page is fitted onto the printable area. The native Windows print
/// dialog (see <see cref="PrintSettings.NativePrinterSettings"/>) has no concept
/// of this — it only covers printer/copies/duplex/paper size — so this is a
/// separate, app-level setting that must be exposed in our own UI.
/// </summary>
public enum PageScaling
{
    /// <summary>
    /// Scale each page down (or up) so it fits entirely within the printer's
    /// printable area. Matches Adobe Acrobat's default "fit to page" behavior.
    /// Maps to PdfiumViewer's <c>PdfPrintMode.ShrinkToMargin</c>.
    /// </summary>
    FitToPage,

    /// <summary>
    /// Print each page at its real size, anchored to the printer's hard margin.
    /// Content that falls outside the printable area will be physically clipped
    /// by the printer. Maps to PdfiumViewer's <c>PdfPrintMode.CutMargin</c>.
    /// </summary>
    ActualSize
}

/// <summary>
/// Printer configuration that applies to all jobs in a batch.
/// Copies/duplex/paper size are normally populated from the native Windows
/// print dialog (see <see cref="NativePrinterSettings"/>) rather than being
/// configured directly in the app UI.
/// </summary>
public class PrintSettings
{
    public string? PrinterName { get; set; }
    public PageOrientation Orientation { get; set; } = PageOrientation.Unknown; // "Auto"
    public OutputColor ColorMode { get; set; } = OutputColor.Color;
    public Duplexing Duplex { get; set; } = Duplexing.OneSided;
    public int Copies { get; set; } = 1;
    public PageMediaSizeName PaperSize { get; set; } = PageMediaSizeName.ISOA4;
    public int DelayBetweenJobsMs { get; set; } = 500;

    /// <summary>
    /// PDF page scaling behavior. Not covered by the native print dialog, so this
    /// must be set explicitly in the app UI. Defaults to FitToPage (the previous
    /// hardcoded behavior), preserving today's working output as the fallback.
    /// </summary>
    public PageScaling Scaling { get; set; } = PageScaling.FitToPage;

    /// <summary>
    /// The native GDI print settings (printer, copies, duplex, paper size, etc.) as
    /// selected by the user via the Windows print dialog. When set, PDF printing uses
    /// this directly instead of re-deriving settings from the fields above.
    /// </summary>
    public System.Drawing.Printing.PrinterSettings? NativePrinterSettings { get; set; }
}
