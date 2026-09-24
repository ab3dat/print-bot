using PrintBot.Models;
using Microsoft.Office.Interop.Word;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;

namespace PrintBot.Services;

/// <summary>
/// Word (.docx, .doc) and Excel (.xlsx, .xls) printing via Office COM Interop.
/// Requires Microsoft Office to be installed.
/// </summary>
public class OfficePrintService : IPrintService
{
    public IReadOnlySet<string> SupportedExtensions { get; } = new HashSet<string>
    {
        "docx", "doc", "xlsx", "xls"
    };

    public async Task<bool> PrintAsync(PrintJob job, PrintSettings settings, CancellationToken ct)
    {
        return await Task.Run(() => PrintOfficeDoc(job, settings), ct);
    }

    private bool PrintOfficeDoc(PrintJob job, PrintSettings settings)
    {
        var ext = Path.GetExtension(job.FullPath).ToLowerInvariant();

        return ext switch
        {
            ".docx" or ".doc" => PrintWord(job, settings),
            ".xlsx" or ".xls" => PrintExcel(job, settings),
            _ => throw new NotSupportedException($"Unsupported Office format: {ext}")
        };
    }

    private bool PrintWord(PrintJob job, PrintSettings settings)
    {
        Application? wordApp = null;
        Document? doc = null;

        try
        {
            wordApp = new Application { Visible = false };
            doc = wordApp.Documents.Open(job.FullPath, ReadOnly: true, Visible: false);

            // Apply print settings
            doc.PageSetup.Orientation = settings.Orientation switch
            {
                PageOrientation.Portrait => WdOrientation.wdOrientPortrait,
                PageOrientation.Landscape => WdOrientation.wdOrientLandscape,
                _ => WdOrientation.wdOrientPortrait
            };

            object background = false;
            object copies = settings.Copies;
            object activePrinter = settings.PrinterName ?? string.Empty;
            object printToFile = false;
            object collate = true;
            object range = WdPrintOutRange.wdPrintAllDocument;
            object items = WdPrintOutItem.wdPrintDocumentContent;
            object pageType = WdPrintOutPages.wdPrintAllPages;
            object? duplex = settings.Duplex switch
            {
                Duplexing.TwoSidedLongEdge => WdTwoOnOneType.wdTwoOnOneNone,
                _ => null
            };

            // Fit to page via "scale to paper size"
            if (settings.Scaling == PageScaling.FitToPage)
            {
                doc.Application.ActivePrinter = settings.PrinterName;
            }

            doc.PrintOut(
                Background: ref background,
                Copies: ref copies,
                ActivePrinterMacGX: ref activePrinter,
                PrintToFile: ref printToFile,
                Collate: ref collate,
                Range: ref range,
                Item: ref items,
                PageType: ref pageType);

            return true;
        }
        finally
        {
            if (doc != null)
            {
                doc.Close(SaveChanges: false);
                Marshal.ReleaseComObject(doc);
            }
            wordApp?.Quit();
            if (wordApp != null) Marshal.ReleaseComObject(wordApp);
        }
    }

    private bool PrintExcel(PrintJob job, PrintSettings settings)
    {
        Application? excelApp = null;
        Workbook? workbook = null;

        try
        {
            excelApp = new Application { Visible = false, DisplayAlerts = false };
            workbook = excelApp.Workbooks.Open(job.FullPath, ReadOnly: true);

            var sheet = (Worksheet)workbook.ActiveSheet;

            // Apply page setup
            sheet.PageSetup.Orientation = settings.Orientation switch
            {
                PageOrientation.Portrait => XlPageOrientation.xlPortrait,
                PageOrientation.Landscape => XlPageOrientation.xlLandscape,
                _ => XlPageOrientation.xlPortrait
            };

            sheet.PageSetup.FitToPagesWide = settings.Scaling == PageScaling.FitToPage ? 1 : 0;
            sheet.PageSetup.FitToPagesTall = settings.Scaling == PageScaling.FitToPage ? 1 : 0;

            if (settings.PaperSize != PageMediaSizeName.Unknown)
            {
                sheet.PageSetup.PaperSize = PaperSizeToExcelPaperSize(settings.PaperSize);
            }

            // Print
            workbook.PrintOut(
                From: 1,
                To: sheet.PageSetup.Pages.Count,
                Copies: settings.Copies,
                ActivePrinter: settings.PrinterName ?? string.Empty,
                Collate: true);

            return true;
        }
        finally
        {
            if (workbook != null)
            {
                workbook.Close(SaveChanges: false);
                Marshal.ReleaseComObject(workbook);
            }
            excelApp?.Quit();
            if (excelApp != null) Marshal.ReleaseComObject(excelApp);
        }
    }

    private static XlPaperSize PaperSizeToExcelPaperSize(PageMediaSizeName name) => name switch
    {
        PageMediaSizeName.ISOA4 => XlPaperSize.xlPaperA4,
        PageMediaSizeName.ISOA3 => XlPaperSize.xlPaperA3,
        PageMediaSizeName.NorthAmericaLetter => XlPaperSize.xlPaperLetter,
        PageMediaSizeName.NorthAmericaLegal => XlPaperSize.xlPaperLegal,
        _ => XlPaperSize.xlPaperA4
    };
}
