using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Pixault.Client;

namespace Pixault.Blazor;

/// <summary>
/// Configure an image's transform parameters and insert it as markdown.
/// Typically shown after selecting an image from <see cref="PixaultGallery"/>.
/// </summary>
public partial class PixaultImageInsert : ComponentBase
{
    [Inject] private PixaultImageService ImageService { get; set; } = default!;
    [Inject] private PixaultAdminClient Admin { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>The project slug (e.g. "barber-shop").</summary>
    [Parameter] public string Project { get; set; } = "";

    /// <summary>The selected image to configure.</summary>
    [Parameter] public ImageMetadataDto? Image { get; set; }

    /// <summary>Fired when the user clicks Insert. Returns the markdown string.</summary>
    [Parameter] public EventCallback<ImageInsertResult> OnInsert { get; set; }

    /// <summary>Fired when the user clicks Cancel.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>Whether to show the Cancel button.</summary>
    [Parameter] public bool ShowCancel { get; set; } = true;

    // Transform state
    private int? _width;
    private int? _height;
    private int? _quality;
    private string _fit = "contain";
    private string _format = "webp";
    private string? _altText;
    private bool _lockAspect = true;

    // Watermark state
    private List<WatermarkDto>? _watermarks;
    private List<object>? _watermarkOptions;
    private string? _watermarkId;
    private string _watermarkPosition = "br";
    private int _watermarkOpacity = 30;

    private readonly List<object> _fitOptions =
    [
        new { Label = "Contain", Value = "contain" },
        new { Label = "Cover", Value = "cover" },
        new { Label = "Fill", Value = "fill" },
        new { Label = "Pad", Value = "pad" },
    ];

    private readonly List<object> _formatOptions =
    [
        new { Label = "WebP", Value = "webp" },
        new { Label = "JPEG", Value = "jpg" },
        new { Label = "PNG", Value = "png" },
        new { Label = "AVIF", Value = "avif" },
    ];

    private readonly List<object> _wmPositionOptions =
    [
        new { Label = "Bottom-Right", Value = "br" },
        new { Label = "Bottom-Left", Value = "bl" },
        new { Label = "Top-Right", Value = "tr" },
        new { Label = "Top-Left", Value = "tl" },
        new { Label = "Center", Value = "c" },
        new { Label = "Tile", Value = "tile" },
    ];

    protected override async Task OnParametersSetAsync()
    {
        if (Image is null) return;

        _altText ??= Image.Caption ?? Image.Name ?? Image.OriginalFileName;

        if (_watermarks is null && !string.IsNullOrEmpty(Project))
        {
            try
            {
                _watermarks = await Admin.ListWatermarksAsync(Project);
                _watermarkOptions = _watermarks
                    .Select(w => (object)new { Label = w.Id, Value = w.Id })
                    .ToList();
            }
            catch
            {
                _watermarks = [];
            }
        }
    }

    private string GeneratedUrl => BuildUrl();

    private string PreviewUrl
    {
        get
        {
            if (Image is null) return "";
            var builder = ImageService.For(Project, Image.ImageId);
            // Preview at reasonable size
            var previewWidth = _width ?? 600;
            builder = builder.Width(Math.Min(previewWidth, 800));
            if (_height.HasValue)
                builder = builder.Height(Math.Min(_height.Value, 600));
            builder = builder.Fit(ParseFit(_fit));
            if (_quality.HasValue)
                builder = builder.Quality(_quality.Value);
            if (_watermarkId is not null)
                builder = builder.Watermark(_watermarkId, ParseWmPosition(_watermarkPosition), _watermarkOpacity);
            builder = builder.Format(_format);
            return builder.Build();
        }
    }

    private string MarkdownSnippet
    {
        get
        {
            var alt = _altText ?? Image?.OriginalFileName ?? "image";
            return $"![{alt}]({GeneratedUrl})";
        }
    }

    private string BuildUrl()
    {
        if (Image is null) return "";
        var builder = ImageService.For(Project, Image.ImageId);

        if (_width.HasValue) builder = builder.Width(_width.Value);
        if (_height.HasValue) builder = builder.Height(_height.Value);
        builder = builder.Fit(ParseFit(_fit));
        if (_quality.HasValue) builder = builder.Quality(_quality.Value);
        if (_watermarkId is not null)
            builder = builder.Watermark(_watermarkId, ParseWmPosition(_watermarkPosition), _watermarkOpacity);
        builder = builder.Format(_format);

        return builder.Build();
    }

    private static WmPosition ParseWmPosition(string pos) => pos switch
    {
        "tl" => WmPosition.TopLeft,
        "tr" => WmPosition.TopRight,
        "bl" => WmPosition.BottomLeft,
        "br" => WmPosition.BottomRight,
        "c" => WmPosition.Center,
        "tile" => WmPosition.Tile,
        _ => WmPosition.BottomRight,
    };

    private static FitMode ParseFit(string fit) => fit switch
    {
        "cover" => FitMode.Cover,
        "contain" => FitMode.Contain,
        "fill" => FitMode.Fill,
        "pad" => FitMode.Pad,
        _ => FitMode.Contain,
    };

    private void OnTransformChanged()
    {
        if (_lockAspect && _width.HasValue && Image is { Width: > 0, Height: > 0 })
        {
            _height = (int)Math.Round((double)_width.Value * Image.Height / Image.Width);
        }
        StateHasChanged();
    }

    private async Task CopyUrlAsync()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", GeneratedUrl);
    }

    private async Task OnInsertClicked()
    {
        var result = new ImageInsertResult
        {
            Markdown = MarkdownSnippet,
            Url = GeneratedUrl,
            AltText = _altText ?? Image?.OriginalFileName ?? "image",
            Image = Image!,
        };
        await OnInsert.InvokeAsync(result);
    }

    private async Task OnCancelClicked()
    {
        await OnCancel.InvokeAsync();
    }
}

/// <summary>
/// Result returned when the user inserts an image via <see cref="PixaultImageInsert"/>.
/// </summary>
public sealed class ImageInsertResult
{
    /// <summary>The full markdown snippet, e.g. ![alt](url).</summary>
    public string Markdown { get; init; } = "";

    /// <summary>The generated CDN URL with transforms.</summary>
    public string Url { get; init; } = "";

    /// <summary>The alt text entered by the user.</summary>
    public string AltText { get; init; } = "";

    /// <summary>The original image metadata.</summary>
    public ImageMetadataDto Image { get; init; } = null!;
}
