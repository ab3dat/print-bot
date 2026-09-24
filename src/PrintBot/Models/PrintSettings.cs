using System.Printing;

namespace PrintBot.Models;

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
    /// The native GDI print settings (printer, copies, duplex, paper size, etc.) as
    /// selected by the user via the Windows print dialog. When set, PDF printing uses
    /// this directly instead of re-deriving settings from the fields above.
    /// </summary>
    public System.Drawing.Printing.PrinterSettings? NativePrinterSettings { get; set; }
}
