using Microsoft.Extensions.DependencyInjection;
using Pixault.Client;

namespace Pixault.Blazor;

/// <summary>
/// Internal helpers for the PixaultImage component.
/// Generates responsive HTML tags using PixaultImageService URLs.
/// </summary>
internal static class ImageTagBuilder
{
    internal static string BuildImgTag(
        PixaultImageService imageService, string project, string imageId,
        string? transform, int? quality, string alt, int[] widths,
        string sizes, string loading, string? cssClass)
    {
        var maxWidth = widths.Max();
        var tp = BuildTransformParams(transform, quality);
        var cls = cssClass is not null ? $" class=\"{Encode(cssClass)}\"" : "";

        var baseUrl = imageService.For(project, imageId).Build();
        var cdnBase = baseUrl[..baseUrl.LastIndexOf('/')];

        var srcset = string.Join(", ",
            widths.OrderBy(w => w).Select(w => $"{cdnBase}/{tp}w_{w}.auto {w}w"));
        var src = $"{cdnBase}/{tp}w_{maxWidth}.auto";

        return $"<img src=\"{src}\" srcset=\"{srcset}\" sizes=\"{Encode(sizes)}\" alt=\"{Encode(alt)}\" width=\"{maxWidth}\" loading=\"{loading}\" decoding=\"async\"{cls}>";
    }

    internal static string BuildPictureTag(
        PixaultImageService imageService, string project, string imageId,
        string? transform, int? quality, string alt, int[] widths,
        string sizes, string loading, string? cssClass)
    {
        var maxWidth = widths.Max();
        var tp = BuildTransformParams(transform, quality);
        var cls = cssClass is not null ? $" class=\"{Encode(cssClass)}\"" : "";

        var baseUrl = imageService.For(project, imageId).Build();
        var cdnBase = baseUrl[..baseUrl.LastIndexOf('/')];

        var avifSrcset = string.Join(", ",
            widths.OrderBy(w => w).Select(w => $"{cdnBase}/{tp}w_{w}.avif {w}w"));
        var webpSrcset = string.Join(", ",
            widths.OrderBy(w => w).Select(w => $"{cdnBase}/{tp}w_{w}.webp {w}w"));
        var fallback = $"{cdnBase}/{tp}w_{maxWidth}.jpg";

        return $"<picture>" +
            $"<source srcset=\"{avifSrcset}\" type=\"image/avif\" sizes=\"{Encode(sizes)}\">" +
            $"<source srcset=\"{webpSrcset}\" type=\"image/webp\" sizes=\"{Encode(sizes)}\">" +
            $"<img src=\"{fallback}\" alt=\"{Encode(alt)}\" width=\"{maxWidth}\" loading=\"{loading}\" decoding=\"async\"{cls}>" +
            $"</picture>";
    }

    private static string BuildTransformParams(string? transform, int? quality)
    {
        var parts = new List<string>();
        if (transform is not null) parts.Add($"t_{transform}");
        if (quality.HasValue) parts.Add($"q_{quality.Value}");
        return parts.Count > 0 ? string.Join(",", parts) + "," : "";
    }

    private static string Encode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);
}

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
