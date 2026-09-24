using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Printing;
using System.Threading;
using System.Threading.Tasks;
using PrintBot.Models;
using PdfiumViewer;

namespace PrintBot.Services;

/// <summary>
/// PDF printing via PdfiumViewer. Supports fit-to-page scaling natively.
/// </summary>
public class PdfPrintService : IPrintService
{
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string> { "pdf" };

    public async Task<bool> PrintAsync(PrintJob job, PrintSettings settings, CancellationToken ct)
    {
        return await Task.Run(() => PrintPdf(job, settings), ct);
    }

    private bool PrintPdf(PrintJob job, PrintSettings settings)
    {
        using var document = PdfDocument.Load(job.FullPath);
        // Scale each page down (if needed) so it fits entirely within the printer's
        // printable area (matches Adobe Acrobat's default "fit to page" behavior).
        // Any driver-level options the user picked in the native print dialog (duplex,
        // quality, paper size, and on printers that support it, scaling) are applied via
        // settings.NativePrinterSettings below and are not overridden here.
        using var printDocument = document.CreatePrintDocument(PdfPrintMode.ShrinkToMargin);

        if (settings.NativePrinterSettings != null)
        {
            // User configured everything (printer, copies, duplex, paper size, ...) via the
            // native Windows print dialog — use it as-is instead of re-deriving settings.
            printDocument.PrinterSettings = settings.NativePrinterSettings;
        }
        else
        {
            // Fallback: derive basic settings from the model (no native dialog was used yet).
            printDocument.PrinterSettings.PrinterName = settings.PrinterName ?? string.Empty;
            printDocument.PrinterSettings.Copies = (short)settings.Copies;
            printDocument.PrinterSettings.DefaultPageSettings.Color = settings.ColorMode == OutputColor.Color;

            printDocument.PrinterSettings.Duplex = settings.Duplex switch
            {
                Duplexing.OneSided => System.Drawing.Printing.Duplex.Simplex,
                Duplexing.TwoSidedLongEdge => System.Drawing.Printing.Duplex.Vertical,
                Duplexing.TwoSidedShortEdge => System.Drawing.Printing.Duplex.Horizontal,
                _ => System.Drawing.Printing.Duplex.Simplex
            };

            if (settings.PaperSize != PageMediaSizeName.Unknown)
            {
                var paperSize = PaperSizeFromMediaSizeName(settings.PaperSize);
                if (paperSize != null)
                {
                    for (int i = 0; i < printDocument.PrinterSettings.PaperSizes.Count; i++)
                    {
                        var ps = printDocument.PrinterSettings.PaperSizes[i];
                        if (ps.Kind == paperSize.Kind && Math.Abs(ps.Width - paperSize.Width) < 5)
                        {
                            printDocument.DefaultPageSettings.PaperSize = ps;
                            break;
                        }
                    }
                }
            }
        }

        // NOTE: document.CreatePrintDocument() already wires up its own PrintPage handler
        // internally that renders each PDF page using PdfiumViewer's native rendering.
        // We intentionally do NOT attach an additional PrintPage handler here — doing so
        // previously caused every page to be drawn twice (once by PdfiumViewer's internal
        // handler, once by our custom scaling code), which looked like a doubled/overlaid
        // printout. Custom scaling (Fit to page / Shrink oversized / Actual size) is left
        // to PdfiumViewer's/Windows' defaults for now.
        printDocument.Print();
        return true;
    }

    private static PaperSize PaperSizeFromMediaSizeName(PageMediaSizeName name)
    {
        return name switch
        {
            PageMediaSizeName.ISOA4 => new PaperSize("A4", 827, 1169),     // 210x297mm in 1/100 inch
            PageMediaSizeName.ISOA3 => new PaperSize("A3", 1169, 1654),
            PageMediaSizeName.NorthAmericaLetter => new PaperSize("Letter", 850, 1100),
            PageMediaSizeName.NorthAmericaLegal => new PaperSize("Legal", 850, 1400),
            _ => new PaperSize(name.ToString(), 827, 1169) // fallback to A4
        };
    }
}
