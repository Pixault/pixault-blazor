using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Pixault.Client;

namespace Pixault.Blazor;

public partial class PixaultImageDetail : ComponentBase
{
    [Inject] private PixaultAdminClient Admin { get; set; } = default!;
    [Inject] private PixaultImageService ImageService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private Radzen.DialogService? Dialog { get; set; }

    [Parameter] public string Project { get; set; } = "";
    [Parameter] public string AccentColor { get; set; } = "#6366f1";
    [Parameter] public ImageMetadataDto? Image { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback<ImageMetadataDto> OnUpdated { get; set; }

    // URL builder state
    private int? _urlWidth;
    private int? _urlHeight;
    private int? _urlQuality;
    private string _urlFormat = "webp";

    // Edit state
    private bool _editing;
    private bool _saving;
    private string? _editName;
    private string? _editDescription;
    private string? _editCaption;
    private string? _editCategory;
    private string? _editFolder;
    private string? _editAuthor;

    // Extended metadata edit state
    private List<string> _editKeywords = [];
    private string _newKeyword = "";
    private string? _editCopyrightHolder;
    private int? _editCopyrightYear;
    private string? _editLicense;
    private DateTimeOffset? _editDateCreated;
    private DateTimeOffset? _editDatePublished;
    private string? _editLocationName;
    private double? _editLocationLat;
    private double? _editLocationLng;
    private bool _editRepresentativeOfPage;
    private Dictionary<string, string> _editTags = new();
    private string _newTagKey = "";
    private string _newTagValue = "";

    // Delete state
    private bool _confirmDelete;
    private bool _deleting;

    // EXIF strip state
    private bool _strippingExif;

    // RootStyle is now inlined in the razor markup

    private static readonly object[] _formatOptions =
    [
        new { Text = "WebP", Value = "webp" },
        new { Text = "JPEG", Value = "jpg" },
        new { Text = "PNG", Value = "png" },
        new { Text = "AVIF", Value = "avif" }
    ];

    private string PreviewUrl
    {
        get
        {
            if (Image is null) return "";
            // For videos, use the thumbnail as the preview/poster image
            var previewId = Image.IsVideo && Image.ThumbnailId is not null
                ? Image.ThumbnailId
                : Image.ImageId;
            return ImageService.For(Project, previewId)
                .Width(600)
                .Height(400)
                .Fit(FitMode.Contain)
                .Format(Image.IsSvg ? "svg" : "webp")
                .Build();
        }
    }

    private string VideoUrl =>
        Image is null ? "" :
        ImageService.VideoUrl(Project, Image.ImageId, Image.ContentType);

    private string ThumbnailUrl =>
        Image?.ThumbnailId is null ? "" :
        ImageService.For(Project, Image.ThumbnailId)
            .Width(640)
            .Format("webp")
            .Build();

    private string GeneratedUrl
    {
        get
        {
            if (Image is null) return "";
            var builder = ImageService.For(Project, Image.ImageId);
            if (_urlWidth.HasValue) builder.Width(_urlWidth.Value);
            if (_urlHeight.HasValue) builder.Height(_urlHeight.Value);
            if (_urlQuality.HasValue) builder.Quality(_urlQuality.Value);
            builder.Format(_urlFormat);
            return builder.Build();
        }
    }

    protected override void OnParametersSet()
    {
        // Reset state when a different image is selected
        _editing = false;
        _confirmDelete = false;
        _urlWidth = null;
        _urlHeight = null;
        _urlQuality = null;
        _urlFormat = "webp";
    }

    private void StartEdit()
    {
        _editName = Image?.Name;
        _editDescription = Image?.Description;
        _editCaption = Image?.Caption;
        _editCategory = Image?.Category;
        _editFolder = Image?.Folder;
        _editAuthor = Image?.Author;
        _editKeywords = Image?.Keywords?.ToList() ?? [];
        _newKeyword = "";
        _editCopyrightHolder = Image?.CopyrightHolder;
        _editCopyrightYear = Image?.CopyrightYear;
        _editLicense = Image?.License;
        _editDateCreated = Image?.DateCreated;
        _editDatePublished = Image?.DatePublished;
        _editLocationName = Image?.LocationName;
        _editLocationLat = Image?.LocationLatitude;
        _editLocationLng = Image?.LocationLongitude;
        _editRepresentativeOfPage = Image?.RepresentativeOfPage ?? false;
        _editTags = Image?.Tags?.ToDictionary(kv => kv.Key, kv => kv.Value) ?? new();
        _newTagKey = "";
        _newTagValue = "";
        _editing = true;
    }

    private void CancelEdit() => _editing = false;

    private async Task SaveMetadataAsync()
    {
        if (Image is null) return;
        _saving = true;

        var update = new MetadataUpdate
        {
            Name = _editName,
            Description = _editDescription,
            Caption = _editCaption,
            Category = _editCategory,
            Folder = _editFolder,
            Author = _editAuthor,
            Keywords = _editKeywords.Count > 0 ? _editKeywords : null,
            CopyrightHolder = _editCopyrightHolder,
            CopyrightYear = _editCopyrightYear,
            License = _editLicense,
            DateCreated = _editDateCreated,
            DatePublished = _editDatePublished,
            RepresentativeOfPage = _editRepresentativeOfPage,
            LocationName = _editLocationName,
            LocationLatitude = _editLocationLat,
            LocationLongitude = _editLocationLng,
            Tags = _editTags.Count > 0 ? _editTags : null
        };

        var updated = await Admin.UpdateMetadataAsync(Image.ImageId, update, project: Project);
        if (updated is not null)
        {
            await OnUpdated.InvokeAsync(updated);
        }

        _saving = false;
        _editing = false;
    }

    private void AddKeyword()
    {
        var kw = _newKeyword.Trim();
        if (!string.IsNullOrEmpty(kw) && !_editKeywords.Contains(kw, StringComparer.OrdinalIgnoreCase))
        {
            _editKeywords.Add(kw);
            _newKeyword = "";
        }
    }

    private void AddTag()
    {
        var key = _newTagKey.Trim();
        var value = _newTagValue.Trim();
        if (!string.IsNullOrEmpty(key) && !_editTags.ContainsKey(key))
        {
            _editTags[key] = value;
            _newTagKey = "";
            _newTagValue = "";
        }
    }

    private async Task ConfirmDeleteAsync()
    {
        if (Image is null) return;
        _deleting = true;

        await Admin.DeleteImageAsync(Image.ImageId, project: Project);
        await OnDeleted.InvokeAsync();
        Dialog?.Close("deleted");

        _deleting = false;
        _confirmDelete = false;
    }

    private async Task StripExifAsync()
    {
        if (Image is null) return;
        _strippingExif = true;

        var updated = await Admin.StripExifAsync(Image.ImageId, project: Project);
        if (updated is not null)
        {
            await OnUpdated.InvokeAsync(updated);
        }

        _strippingExif = false;
    }

    private async Task CopyToClipboard(string text)
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }

    // Simple helper component rendered inline with inline styles
    private static RenderFragment MetaRow(string label, string? value) => builder =>
    {
        if (string.IsNullOrEmpty(value)) return;

        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "style", "color: #6b7280; font-weight: 500; font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.03em;");
        builder.AddContent(2, label);
        builder.CloseElement();

        builder.OpenElement(3, "span");
        builder.AddAttribute(4, "style", "color: #1f2937;");
        builder.AddContent(5, value);
        builder.CloseElement();
    };
}
