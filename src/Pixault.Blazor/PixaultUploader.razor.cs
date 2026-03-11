using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Pixault.Blazor.Models;
using Pixault.Client;

namespace Pixault.Blazor;

public partial class PixaultUploader : ComponentBase
{
    [Inject] private PixaultUploadClient UploadClient { get; set; } = null!;
    [Inject] private PixaultImageService ImageService { get; set; } = null!;
    [Inject] private PixaultAdminClient AdminClient { get; set; } = null!;

    /// <summary>
    /// Project identifier for uploads (e.g., "barber", "tattoo").
    /// </summary>
    [Parameter, EditorRequired] public string Project { get; set; } = "";

    /// <summary>
    /// Accent color for the widget (CSS color value).
    /// </summary>
    [Parameter] public string AccentColor { get; set; } = "#6366f1";

    /// <summary>
    /// Maximum number of files allowed per upload session.
    /// </summary>
    [Parameter] public int MaxFiles { get; set; } = 10;

    /// <summary>
    /// Maximum file size in megabytes.
    /// </summary>
    [Parameter] public int MaxSizeMb { get; set; } = 20;

    /// <summary>
    /// Accepted file extensions (without dots).
    /// </summary>
    [Parameter] public string[] Accept { get; set; } = ["jpg", "jpeg", "png", "webp", "svg"];

    /// <summary>
    /// Whether to allow multiple file selection.
    /// </summary>
    [Parameter] public bool Multiple { get; set; } = true;

    /// <summary>
    /// Whether to show image previews after upload.
    /// </summary>
    [Parameter] public bool ShowPreview { get; set; } = true;

    /// <summary>
    /// Pixault transformation string for preview thumbnails (e.g., "w_200,h_200").
    /// </summary>
    [Parameter] public string PreviewTransform { get; set; } = "w_200,h_200";

    /// <summary>
    /// Fired when a file upload completes successfully.
    /// </summary>
    [Parameter] public EventCallback<UploadCompleteEventArgs> OnUploadComplete { get; set; }

    /// <summary>
    /// Optional custom content for the drop zone.
    /// </summary>
    [Parameter] public RenderFragment? DropZoneContent { get; set; }

    /// <summary>
    /// When set, the uploader runs in embedded mode: the folder selector is hidden
    /// and uploads go directly to this folder.
    /// </summary>
    [Parameter] public string? InitialFolder { get; set; }

    private readonly List<UploadItem> _items = [];
    private bool _isDragging;
    private int _dragCounter;
    private List<string> _folders = [];
    private string? _selectedFolder;

    protected override async Task OnInitializedAsync()
    {
        if (InitialFolder is not null)
        {
            _selectedFolder = InitialFolder;
            return;
        }

        try
        {
            _folders = await AdminClient.ListFoldersAsync(project: Project);
        }
        catch
        {
            _folders = [];
        }
    }

    private object[] _folderOptions => _folders.Select(f => new { Text = f, Value = f }).ToArray<object>();

    private string AcceptString =>
        string.Join(",", Accept.Select(ext => $".{ext.TrimStart('.')}"));

    private string AcceptHint =>
        string.Join(", ", Accept.Select(ext => ext.TrimStart('.').ToUpperInvariant()));

    // RootStyle and DropzoneClass are now inlined in the razor markup

    private static string FileCardBorderColor(UploadItem item) => item.State switch
    {
        UploadState.Complete => "#10b981",
        UploadState.Error => "#ef4444",
        UploadState.Uploading => "var(--pxlt-accent, #06b6d4)",
        _ => "#e5e7eb"
    };

    private static string FileCardBgColor(UploadItem item) => item.State switch
    {
        UploadState.Complete => "rgba(16, 185, 129, 0.04)",
        UploadState.Error => "rgba(239, 68, 68, 0.04)",
        _ => "#fff"
    };

    private void HandleDragEnter()
    {
        _dragCounter++;
        _isDragging = true;
    }

    private void HandleDragLeave()
    {
        _dragCounter--;
        if (_dragCounter <= 0)
        {
            _dragCounter = 0;
            _isDragging = false;
        }
    }

    private async Task HandleFilesSelected(InputFileChangeEventArgs e)
    {
        _isDragging = false;
        _dragCounter = 0;

        var files = Multiple ? e.GetMultipleFiles(MaxFiles) : [e.File];

        foreach (var file in files)
        {
            if (_items.Count >= MaxFiles)
                break;

            var item = new UploadItem { BrowserFile = file };
            _items.Add(item);
        }

        StateHasChanged();

        // Upload all pending items
        foreach (var item in _items.Where(i => i.State == UploadState.Pending).ToList())
        {
            await UploadFileAsync(item);
        }
    }

    private async Task UploadFileAsync(UploadItem item)
    {
        try
        {
            // Validate
            item.State = UploadState.Validating;
            item.Progress = 0;
            StateHasChanged();

            if (item.SizeBytes > MaxSizeMb * 1024L * 1024)
            {
                item.State = UploadState.Error;
                item.ErrorMessage = $"File exceeds {MaxSizeMb} MB limit";
                StateHasChanged();
                return;
            }

            var ext = Path.GetExtension(item.FileName).TrimStart('.').ToLowerInvariant();
            if (!Accept.Any(a => a.TrimStart('.').Equals(ext, StringComparison.OrdinalIgnoreCase)))
            {
                item.State = UploadState.Error;
                item.ErrorMessage = $"File type .{ext} is not accepted";
                StateHasChanged();
                return;
            }

            // Read file with progress tracking
            item.State = UploadState.Uploading;
            StateHasChanged();

            using var ms = new MemoryStream();
            await using var stream = item.BrowserFile.OpenReadStream(maxAllowedSize: MaxSizeMb * 1024L * 1024);

            var buffer = new byte[8192];
            var totalRead = 0L;
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
            {
                await ms.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;
                item.Progress = (double)totalRead / item.SizeBytes * 80;
                StateHasChanged();
            }

            // Upload to Pixault
            item.Progress = 85;
            StateHasChanged();

            ms.Position = 0;
            var response = await UploadClient.UploadAsync(
                Project, item.FileName, ms, item.ContentType, _selectedFolder);

            // Complete
            item.Progress = 100;
            item.State = UploadState.Complete;
            item.ImageId = response.ImageId;
            item.Url = response.Url;

            if (ShowPreview)
            {
                var previewFormat = item.IsSvg ? "svg" : "webp";
                item.PreviewUrl = ImageService.For(Project, response.ImageId)
                    .Format(previewFormat)
                    .Build()
                    .Replace("/original.", $"/{PreviewTransform}.");
            }

            StateHasChanged();

            await OnUploadComplete.InvokeAsync(new UploadCompleteEventArgs
            {
                ImageId = response.ImageId,
                Url = response.Url,
                FileName = item.FileName,
                ContentType = item.ContentType,
                Width = item.Width,
                Height = item.Height,
                PreviewUrl = item.PreviewUrl,
                IsSvg = item.IsSvg
            });
        }
        catch (Exception ex)
        {
            item.State = UploadState.Error;
            item.ErrorMessage = ex.Message;
            StateHasChanged();
        }
    }

    private async Task RetryUploadAsync(UploadItem item)
    {
        item.State = UploadState.Pending;
        item.ErrorMessage = null;
        item.Progress = 0;
        StateHasChanged();
        await UploadFileAsync(item);
    }

    private void RemoveItem(UploadItem item)
    {
        _items.Remove(item);
        StateHasChanged();
    }
}
