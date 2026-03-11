using Microsoft.AspNetCore.Components;
using Pixault.Client;

namespace Pixault.Blazor;

public partial class PixaultGallery : ComponentBase
{
    [Inject] private PixaultAdminClient Admin { get; set; } = default!;
    [Inject] private PixaultImageService ImageService { get; set; } = default!;

    [Parameter] public string Project { get; set; } = "";
    [Parameter] public string AccentColor { get; set; } = "#6366f1";
    [Parameter] public string ThumbnailTransform { get; set; } = "w_240,h_240,fit_cover";
    [Parameter] public EventCallback<ImageMetadataDto> OnImageSelected { get; set; }

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

    private async Task CreateFolderAsync()
    {
        var name = _newFolderName.Trim().Trim('/');
        if (string.IsNullOrWhiteSpace(name)) return;

        var fullPath = string.IsNullOrEmpty(_currentPath) ? name : $"{_currentPath}/{name}";
        await Admin.CreateFolderAsync(fullPath, project: Project);
        _newFolderName = "";
        _creatingFolder = false;
        await LoadFoldersAsync();
        StateHasChanged();
    }

    private async Task DeleteFolderAsync(string folder)
    {
        await Admin.DeleteFolderAsync(folder, project: Project);
        if (_currentPath == folder)
            _currentPath = "";
        await Task.WhenAll(LoadFoldersAsync(), LoadImagesAsync());
        StateHasChanged();
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
        var thumbId = image.IsVideo && image.ThumbnailId is not null
            ? image.ThumbnailId
            : image.ImageId;

        return ImageService.For(Project, thumbId)
            .Width(240)
            .Height(240)
            .Fit(FitMode.Cover)
            .Format(image.IsSvg ? "svg" : "webp")
            .Build();
    }
}
