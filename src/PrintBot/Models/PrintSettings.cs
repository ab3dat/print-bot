using System.Printing;

namespace PrintBot.Models;

/// <summary>
/// Printer configuration that applies to all jobs in a batch.
/// </summary>
public class PrintSettings
{
    public string? PrinterName { get; set; }
    public PageScaling Scaling { get; set; } = PageScaling.FitToPage;
    public PageOrientation Orientation { get; set; } = PageOrientation.Unknown; // "Auto"
    public OutputColor ColorMode { get; set; } = OutputColor.Color;
    public Duplexing Duplex { get; set; } = Duplexing.OneSided;
    public int Copies { get; set; } = 1;
    public PageMediaSizeName PaperSize { get; set; } = PageMediaSizeName.ISOA4;
    public int DelayBetweenJobsMs { get; set; } = 500;
}

public enum PageScaling
{
    FitToPage,
    ActualSize,
    ShrinkOversized
}

public static class PrintSettingsDefaults
{
    public static List<PageScaling> DefaultScalingOptions { get; } = new()
    {
        PageScaling.FitToPage,
        PageScaling.ActualSize,
        PageScaling.ShrinkOversized
    };
}
