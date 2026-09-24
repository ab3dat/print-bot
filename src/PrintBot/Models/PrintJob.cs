namespace PrintBot.Models;

/// <summary>
/// Core domain model for a single print job in the queue.
/// </summary>
public partial class PrintJob
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];

    public string FileName { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;  // pdf, docx, xlsx, etc.
    public int? EstimatedPages { get; set; }

    public PrintJobStatus Status { get; set; } = PrintJobStatus.Queued;
    public string? ErrorMessage { get; set; }
    public DateTime? PrintedAt { get; set; }
}

public enum PrintJobStatus
{
    Queued,
    Printing,
    Printed,
    Failed,
    Skipped
}
