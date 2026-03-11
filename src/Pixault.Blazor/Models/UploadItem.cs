using Microsoft.AspNetCore.Components.Forms;

namespace Pixault.Blazor.Models;

public enum UploadState
{
    Pending,
    Validating,
    Uploading,
    Complete,
    Error
}

public sealed class UploadItem
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public IBrowserFile BrowserFile { get; init; } = null!;
    public string FileName => BrowserFile.Name;
    public long SizeBytes => BrowserFile.Size;
    public string ContentType => BrowserFile.ContentType;

    public UploadState State { get; set; } = UploadState.Pending;
    public double Progress { get; set; }
    public string? ErrorMessage { get; set; }

    public string? ImageId { get; set; }
    public string? Url { get; set; }
    public string? PreviewUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public bool IsSvg => ContentType == "image/svg+xml";

    public string FormattedSize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024):F1} MB"
    };
}
