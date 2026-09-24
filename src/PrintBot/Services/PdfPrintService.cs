using System.Drawing.Printing;
using System.Printing;
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
        using var printDocument = document.CreatePrintDocument();

        // Configure the print document
        printDocument.PrinterSettings.PrinterName = settings.PrinterName ?? string.Empty;
        printDocument.PrinterSettings.Copies = (short)settings.Copies;
        printDocument.PrinterSettings.DefaultPageSettings.Color = settings.ColorMode == OutputColor.Color;

        // Apply duplex setting
        printDocument.PrinterSettings.Duplex = settings.Duplex switch
        {
            Duplexing.OneSided => System.Drawing.Printing.Duplex.Simplex,
            Duplexing.TwoSidedLongEdge => System.Drawing.Printing.Duplex.Vertical,
            Duplexing.TwoSidedShortEdge => System.Drawing.Printing.Duplex.Horizontal,
            _ => System.Drawing.Printing.Duplex.Simplex
        };

        // Set paper size if specified
        if (settings.PaperSize != PageMediaSizeName.Unknown)
        {
            var paperSize = PaperSizeFromMediaSizeName(settings.PaperSize);
            if (paperSize != null)
            {
                // Iterate through available paper sizes
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

        // Page scaling logic
        int currentPage = 0;
        printDocument.PrintPage += (_, e) =>
        {
            if (currentPage >= document.PageCount)
            {
                e.HasMorePages = false;
                return;
            }

            var page = document.Render(
                currentPage,
                e.PageBounds.Width,
                e.PageBounds.Height,
                PdfRenderFlags.CorrectFromDpi | PdfRenderFlags.Annotations);

            // Fit to page: scale image to fit printable area
            if (settings.Scaling == PageScaling.FitToPage)
            {
                var marginBounds = e.MarginBounds;
                float scaleX = (float)marginBounds.Width / page.Width;
                float scaleY = (float)marginBounds.Height / page.Height;
                float scale = Math.Min(scaleX, scaleY);

                int destWidth = (int)(page.Width * scale);
                int destHeight = (int)(page.Height * scale);
                int destX = marginBounds.X + (marginBounds.Width - destWidth) / 2;
                int destY = marginBounds.Y + (marginBounds.Height - destHeight) / 2;

                e.Graphics!.DrawImage(page, destX, destY, destWidth, destHeight);
            }
            else if (settings.Scaling == PageScaling.ShrinkOversized)
            {
                var marginBounds = e.MarginBounds;
                float scale = Math.Min(1f, Math.Min(
                    (float)marginBounds.Width / page.Width,
                    (float)marginBounds.Height / page.Height));

                int destWidth = (int)(page.Width * scale);
                int destHeight = (int)(page.Height * scale);
                int destX = marginBounds.X + (marginBounds.Width - destWidth) / 2;
                int destY = marginBounds.Y + (marginBounds.Height - destHeight) / 2;

                e.Graphics!.DrawImage(page, destX, destY, destWidth, destHeight);
            }
            else
            {
                // Actual size — draw at 1:1 from top-left
                e.Graphics!.DrawImage(page, e.MarginBounds.X, e.MarginBounds.Y, page.Width, page.Height);
            }

            page.Dispose();
            currentPage++;
            e.HasMorePages = currentPage < document.PageCount;
        };

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
