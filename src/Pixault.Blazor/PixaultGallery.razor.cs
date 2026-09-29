using Microsoft.AspNetCore.Components;
using Pixault.Client;

namespace Pixault.Blazor;

public partial class PixaultGallery : ComponentBase
{
    [Inject] private IPixaultAdminClient Admin { get; set; } = default!;
    [Inject] private PixaultImageService ImageService { get; set; } = default!;

    [Parameter] public string Project { get; set; } = "";
    [Parameter] public string AccentColor { get; set; } = "#6366f1";
    [Parameter] public string ThumbnailTransform { get; set; } = "w_400";
    [Parameter] public EventCallback<ImageMetadataDto> OnImageSelected { get; set; }

    /// <summary>Whether to show the bulk-management affordances: the per-card tick boxes,
    /// "select all", and the Move/Delete bar they drive. Leave true for a management gallery.
    /// Set false when the gallery is a picker (an image-chooser dialog): there, a tick box reads
    /// as "choose this one" but actually arms a bulk Delete, and it never raises
    /// <see cref="OnImageSelected"/> because the tick box stops the click reaching the card.</summary>
    [Parameter] public bool AllowBulkActions { get; set; } = true;

    /// <summary>Max files selectable in the inline upload panel. A management gallery expects bulk
    /// uploads, so this defaults high; lower it for consumers that want a tighter cap.</summary>
    [Parameter] public int MaxFiles { get; set; } = 1000;

    /// <summary>Max size (MB) of a single upload in the inline panel. Forwarded to the uploader's
    /// per-file cap; raise for large images/EPS/vector inputs, lower to restrict.</summary>
    [Parameter] public int MaxSizeMb { get; set; } = 100;

    private List<ImageMetadataDto> _images = [];
    private List<ImageMetadataDto> _filtered = [];
    private List<string> _folders = [];
    private string _currentPath = ""; // "" = root
    private List<string> _childFolders = [];
    private bool _creatingFolder;
    private string _newFolderName = "";
    private bool _showUploadZone;
    private string? _searchTerm;
    private string? _nextCursor;
    private int _totalCount;
    private bool _loading = true;
    private bool _loadingMore;
    private string? _selectedId;
    private string? _error;

    // ── Multi-select + view mode ──
    private enum GalleryView { Grid, List }
    private GalleryView _view = GalleryView.Grid;
    private readonly HashSet<string> _selected = new(StringComparer.Ordinal);

    private bool IsSelected(string id) => _selected.Contains(id);

    private void ToggleSelected(string id)
    {
        if (!_selected.Add(id)) _selected.Remove(id);
    }

    private void ClearSelection() => _selected.Clear();

    private bool AllVisibleSelected => _filtered.Count > 0 && _filtered.All(i => _selected.Contains(i.ImageId));

    private void ToggleSelectAllVisible()
    {
        if (AllVisibleSelected)
            foreach (var i in _filtered) _selected.Remove(i.ImageId);
        else
            foreach (var i in _filtered) _selected.Add(i.ImageId);
    }

    // ── Bulk operations ──
    private bool _showBulkDelete;
    private bool _showMove;
    private string _moveTarget = ""; // "" = root
    private bool _bulkBusy;

    private void OpenMoveDialog()
    {
        _moveTarget = "";
        _showMove = true;
    }

    private async Task ConfirmBulkDeleteAsync()
    {
        _bulkBusy = true;
        StateHasChanged();
        foreach (var id in _selected.ToList())
        {
            try { await Admin.DeleteImageAsync(id, project: Project); }
            catch { /* best-effort; reload reflects what actually happened */ }
        }
        _showBulkDelete = false;
        _bulkBusy = false;
        await Task.WhenAll(LoadImagesAsync(), LoadFoldersAsync());
        StateHasChanged();
    }

    private async Task ConfirmBulkMoveAsync()
    {
        _bulkBusy = true;
        StateHasChanged();
        // Folder = "" moves to root (endpoint maps empty → null); "path" moves into that folder.
        foreach (var id in _selected.ToList())
        {
            try { await Admin.UpdateMetadataAsync(id, new MetadataUpdate { Folder = _moveTarget }, project: Project); }
            catch { /* best-effort */ }
        }
        _showMove = false;
        _bulkBusy = false;
        await Task.WhenAll(LoadImagesAsync(), LoadFoldersAsync());
        StateHasChanged();
    }

    private List<(string Name, string Path)> BreadcrumbSegments
    {
        get
        {
            var segments = new List<(string Name, string Path)> { ("Root", "") };
            if (string.IsNullOrEmpty(_currentPath)) return segments;

            var parts = _currentPath.Split('/');
            for (var i = 0; i < parts.Length; i++)
            {
                var path = string.Join("/", parts[..(i + 1)]);
                segments.Add((parts[i], path));
            }
            return segments;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadImagesAsync(), LoadFoldersAsync());
    }

    private async Task LoadFoldersAsync()
    {
        try
        {
            _folders = await Admin.ListFoldersAsync(project: Project);
        }
        catch
        {
            _folders = [];
        }
        ComputeChildFolders();
    }

    private void ComputeChildFolders()
    {
        var prefix = string.IsNullOrEmpty(_currentPath) ? "" : _currentPath + "/";
        _childFolders = _folders
            .Where(f => string.IsNullOrEmpty(prefix)
                ? !f.Contains('/')                     // root: only top-level folders
                : f.StartsWith(prefix) && !f[prefix.Length..].Contains('/')) // nested: direct children only
            .ToList();
    }

    private async Task NavigateToFolder(string path)
    {
        _currentPath = path;
        ComputeChildFolders();
        await LoadImagesAsync();
    }

    private string ChildFolderDisplayName(string fullPath)
    {
        var idx = fullPath.LastIndexOf('/');
        return idx >= 0 ? fullPath[(idx + 1)..] : fullPath;
    }

    private async Task LoadImagesAsync()
    {
        _loading = true;
        _error = null;
        _nextCursor = null;
        _selected.Clear(); // fresh context (folder change / search / refresh) — drop stale selections
        StateHasChanged();

        try
        {
            var search = string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm.Trim();
            var result = await Admin.ListImagesAsync(50, project: Project, search: search, folder: _currentPath);
            _images = result.Images;
            _filtered = _images;
            _nextCursor = result.NextCursor;
            _totalCount = result.TotalCount;
        }
        catch (HttpRequestException ex)
        {
            _error = $"Failed to load images: {ex.StatusCode?.ToString() ?? ex.Message}";
            _images = [];
            _filtered = [];
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadMoreAsync()
    {
        if (_nextCursor is null) return;
        _loadingMore = true;

        var search = string.IsNullOrWhiteSpace(_searchTerm) ? null : _searchTerm.Trim();
        var result = await Admin.ListImagesAsync(50, _nextCursor, project: Project, search: search, folder: _currentPath);
        _images.AddRange(result.Images);
        _filtered = _images;
        _nextCursor = result.NextCursor;
        _totalCount = result.TotalCount;
        _loadingMore = false;
    }

    private async Task OnSearchChanged()
    {
        await LoadImagesAsync();
    }

    private async Task OnFolderNameKeyDown(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            await CreateFolderAsync();
        else if (e.Key == "Escape")
        {
            _creatingFolder = false;
            _newFolderName = "";
        }
    }

    private async Task CreateFolderAsync()
    {
        var name = _newFolderName.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var fullPath = string.IsNullOrEmpty(_currentPath) ? name : $"{_currentPath}/{name}";
            await Admin.CreateFolderAsync(fullPath, project: Project);
            _newFolderName = "";
            _creatingFolder = false;
            await LoadFoldersAsync();
        }
        catch (Exception ex)
        {
            _error = $"Failed to create folder: {ex.Message}";
        }
        StateHasChanged();
    }

    private string? _folderToDelete;
    private bool _deletingFolder;

    private async Task ConfirmDeleteFolderAsync()
    {
        if (_folderToDelete is null) return;
        _deletingFolder = true;
        StateHasChanged();

        try
        {
            await Admin.DeleteFolderAsync(_folderToDelete, project: Project);
            if (_currentPath == _folderToDelete)
                _currentPath = "";
            _folderToDelete = null;
            await Task.WhenAll(LoadFoldersAsync(), LoadImagesAsync());
        }
        finally
        {
            _deletingFolder = false;
            StateHasChanged();
        }
    }

    private async Task OnInlineUploadComplete()
    {
        await LoadImagesAsync();
    }

    private void SelectImage(ImageMetadataDto image)
    {
        _selectedId = image.ImageId;
        OnImageSelected.InvokeAsync(image);
    }

    private string ThumbnailUrl(ImageMetadataDto image)
    {
        // Use ThumbnailId for videos and EPS files (points to derived rasterized image)
        var thumbId = (image.IsVideo || image.IsEps) && image.ThumbnailId is not null
            ? image.ThumbnailId
            : image.ImageId;

        return ImageService.For(Project, thumbId)
            .Width(400)
            .Format(image.IsSvg ? "svg" : "webp")
            .Build();
    }
}
