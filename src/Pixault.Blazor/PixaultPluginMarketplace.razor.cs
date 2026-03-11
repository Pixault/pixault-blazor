using Microsoft.AspNetCore.Components;
using Pixault.Client;

namespace Pixault.Blazor;

public partial class PixaultPluginMarketplace : ComponentBase
{
    [Parameter] public string ProjectId { get; set; } = "default";

    private List<ProjectPluginDto> Plugins { get; set; } = [];
    private IEnumerable<ProjectPluginDto> FilteredPlugins =>
        ActiveFilter is null ? Plugins : Plugins.Where(p => p.Category == ActiveFilter);

    private List<string> Categories { get; set; } = [];
    private string? ActiveFilter { get; set; }
    private bool Loading { get; set; } = true;
    private string? Error { get; set; }
    private HashSet<string> Toggling { get; } = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadPluginsAsync();
    }

    private async Task LoadPluginsAsync()
    {
        try
        {
            Loading = true;
            Error = null;

            Plugins = await Admin.GetProjectPluginsAsync(ProjectId);
            Categories = Plugins.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Loading = false;
        }
    }

    private void SetFilter(string? category)
    {
        ActiveFilter = category;
    }

    private async Task TogglePluginAsync(ProjectPluginDto plugin)
    {
        Toggling.Add(plugin.Name);
        StateHasChanged();

        try
        {
            if (plugin.IsActivated)
            {
                await Admin.DeactivatePluginAsync(ProjectId, plugin.Name);
                plugin.IsActivated = false;
            }
            else
            {
                await Admin.ActivatePluginAsync(ProjectId, plugin.Name);
                plugin.IsActivated = true;
            }
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            Toggling.Remove(plugin.Name);
        }
    }

    private static string GetCategoryIcon(string category)
    {
        return category switch
        {
            "Enhancement" => "\U0001f3a8",
            "Composition" => "\U0001f5bc\ufe0f",
            "Branding" => "\U0001f3f7\ufe0f",
            "Effects" => "\u2728",
            _ => "\U0001f9e9"
        };
    }
}
