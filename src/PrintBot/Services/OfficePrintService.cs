using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Printing;
using PrintBot.Models;
using Word = Microsoft.Office.Interop.Word;
using Excel = Microsoft.Office.Interop.Excel;

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
        Word.Application? wordApp = null;
        Word.Document? doc = null;

        try
        {
            wordApp = new Word.Application { Visible = false };
            doc = wordApp.Documents.Open(job.FullPath, ReadOnly: true, Visible: false);

            // Apply print settings
            doc.PageSetup.Orientation = settings.Orientation switch
            {
                PageOrientation.Portrait => Word.WdOrientation.wdOrientPortrait,
                PageOrientation.Landscape => Word.WdOrientation.wdOrientLandscape,
                _ => Word.WdOrientation.wdOrientPortrait
            };

            object background = false;
            object copies = settings.Copies;
            object activePrinter = settings.PrinterName ?? string.Empty;
            object printToFile = false;
            object collate = true;
            object range = Word.WdPrintOutRange.wdPrintAllDocument;
            object items = Word.WdPrintOutItem.wdPrintDocumentContent;
            object pageType = Word.WdPrintOutPages.wdPrintAllPages;

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
        Excel.Application? excelApp = null;
        Excel.Workbook? workbook = null;

        try
        {
            excelApp = new Excel.Application { Visible = false, DisplayAlerts = false };
            workbook = excelApp.Workbooks.Open(job.FullPath, ReadOnly: true);

            var sheet = (Excel.Worksheet)workbook.ActiveSheet;

            // Apply page setup
            sheet.PageSetup.Orientation = settings.Orientation switch
            {
                PageOrientation.Portrait => Excel.XlPageOrientation.xlPortrait,
                PageOrientation.Landscape => Excel.XlPageOrientation.xlLandscape,
                _ => Excel.XlPageOrientation.xlPortrait
            };

            sheet.PageSetup.FitToPagesWide = 1;
            sheet.PageSetup.FitToPagesTall = 1;

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

    private static Excel.XlPaperSize PaperSizeToExcelPaperSize(PageMediaSizeName name) => name switch
    {
        PageMediaSizeName.ISOA4 => Excel.XlPaperSize.xlPaperA4,
        PageMediaSizeName.ISOA3 => Excel.XlPaperSize.xlPaperA3,
        PageMediaSizeName.NorthAmericaLetter => Excel.XlPaperSize.xlPaperLetter,
        PageMediaSizeName.NorthAmericaLegal => Excel.XlPaperSize.xlPaperLegal,
        _ => Excel.XlPaperSize.xlPaperA4
    };
}
