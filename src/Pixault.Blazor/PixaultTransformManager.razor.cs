using Microsoft.AspNetCore.Components;
using Pixault.Client;

namespace Pixault.Blazor;

public partial class PixaultTransformManager : ComponentBase
{
    [Inject] private PixaultAdminClient Admin { get; set; } = default!;

    [Parameter] public string Project { get; set; } = "";
    [Parameter] public string AccentColor { get; set; } = "#6366f1";

    private List<NamedTransformDto> _transforms = [];
    private List<WatermarkDto> _watermarks = [];
    private bool _loading = true;
    private bool _showForm;
    private bool _saving;
    private string? _formError;
    private string _lastProject = "";

    // Form fields
    private string? _editingName; // null = create, non-null = edit
    private string _formName = "";
    private int? _formWidth;
    private int? _formHeight;
    private string _formFitMode = "";
    private int? _formQuality;
    private int? _formBlur;
    private string _formWatermarkId = "";
    private string _formWatermarkPosition = "";
    private int? _formWatermarkOpacity;

    private static readonly object[] _fitModes =
    [
        new { Text = "Cover", Value = "Cover" },
        new { Text = "Contain", Value = "Contain" },
        new { Text = "Fill", Value = "Fill" },
        new { Text = "Pad", Value = "Pad" },
    ];

    private static readonly object[] _watermarkPositions =
    [
        new { Text = "Top Left", Value = "tl" },
        new { Text = "Top Right", Value = "tr" },
        new { Text = "Bottom Left", Value = "bl" },
        new { Text = "Bottom Right", Value = "br" },
        new { Text = "Center", Value = "c" },
        new { Text = "Tile", Value = "tile" },
    ];

    private string RootStyle => $"--pxlt-accent: {AccentColor};";

    protected override async Task OnInitializedAsync()
    {
        _lastProject = Project;
        await LoadTransformsAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Project != _lastProject)
        {
            _lastProject = Project;
            _showForm = false;
            await LoadTransformsAsync();
        }
    }

    private async Task LoadTransformsAsync()
    {
        _loading = true;
        _transforms = await Admin.ListTransformsAsync(project: Project);
        try
        {
            _watermarks = await Admin.ListWatermarksAsync(project: Project);
        }
        catch
        {
            _watermarks = [];
        }
        _loading = false;
    }

    private void StartCreate()
    {
        _editingName = null;
        _formName = "";
        _formWidth = null;
        _formHeight = null;
        _formFitMode = "";
        _formQuality = null;
        _formBlur = null;
        _formWatermarkId = "";
        _formWatermarkPosition = "";
        _formWatermarkOpacity = null;
        _formError = null;
        _showForm = true;
    }

    private void StartEdit(NamedTransformDto t)
    {
        _editingName = t.Name;
        _formName = t.Name;
        _formWidth = t.Width;
        _formHeight = t.Height;
        _formFitMode = t.FitMode ?? "";
        _formQuality = t.Quality;
        _formBlur = t.Blur;
        _formWatermarkId = t.WatermarkId ?? "";
        _formWatermarkPosition = t.WatermarkPosition ?? "";
        _formWatermarkOpacity = t.WatermarkOpacity;
        _formError = null;
        _showForm = true;
    }

    private void CancelForm()
    {
        _showForm = false;
        _formError = null;
    }

    private async Task SaveTransformAsync()
    {
        var name = _editingName ?? _formName;
        if (string.IsNullOrWhiteSpace(name))
        {
            _formError = "Name is required.";
            return;
        }

        _saving = true;
        _formError = null;

        try
        {
            var save = new NamedTransformSave
            {
                Width = _formWidth,
                Height = _formHeight,
                FitMode = string.IsNullOrEmpty(_formFitMode) ? null : _formFitMode,
                Quality = _formQuality,
                Blur = _formBlur,
                WatermarkId = string.IsNullOrEmpty(_formWatermarkId) ? null : _formWatermarkId,
                WatermarkPosition = string.IsNullOrEmpty(_formWatermarkPosition) ? null : _formWatermarkPosition,
                WatermarkOpacity = _formWatermarkOpacity,
            };

            await Admin.SaveTransformAsync(name, save, project: Project);
            await LoadTransformsAsync();
            _showForm = false;
        }
        catch (HttpRequestException ex)
        {
            _formError = $"Failed to save: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task DeleteTransformAsync(string name)
    {
        await Admin.DeleteTransformAsync(name, project: Project);
        _transforms.RemoveAll(t => t.Name == name);
    }
}
