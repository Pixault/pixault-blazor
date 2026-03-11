namespace Pixault.Blazor.Models;

public sealed class UploadCompleteEventArgs
{
    public required string ImageId { get; init; }
    public required string Url { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public string? PreviewUrl { get; init; }
    public bool IsSvg { get; init; }
}
