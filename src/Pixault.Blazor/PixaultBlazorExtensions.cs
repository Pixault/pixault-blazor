using Microsoft.Extensions.DependencyInjection;
using Pixault.Client;

namespace Pixault.Blazor;

/// <summary>
/// DI extensions for Pixault Blazor components.
/// Call <c>AddPixault()</c> first (from Pixault.Client), then <c>AddPixaultBlazor()</c>.
/// </summary>
public static class PixaultBlazorExtensions
{
    /// <summary>
    /// Registers Pixault Blazor upload widget services.
    /// Requires <see cref="PixaultServiceExtensions.AddPixault"/> to be called first.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddPixault(options => { ... });
    /// builder.Services.AddPixaultBlazor();
    /// </code>
    /// </example>
    public static IServiceCollection AddPixaultBlazor(this IServiceCollection services)
    {
        // PixaultUploadClient and PixaultImageService are already registered
        // by AddPixault(). This method is a forward-compatible extension point
        // for any Blazor-specific services (e.g., JS interop modules).
        return services;
    }
}
