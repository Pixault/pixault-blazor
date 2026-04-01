# Pixault.Blazor

Blazor component library for the [Pixault](https://pixault.io) image processing CDN.

## Components

| Component | Description |
|-----------|-------------|
| `PixaultUploader` | Drag-and-drop file uploader with progress tracking and previews |
| `PixaultGallery` | Image gallery with folder navigation, search, and breadcrumbs |
| `PixaultImageDetail` | Image detail view with metadata, URL builder, and edit controls |
| `PixaultTransformManager` | Named transform (preset) management UI |
| `PixaultPluginMarketplace` | Plugin discovery and installation interface |

## Installation

```bash
dotnet add package Pixault.Blazor
```

## Setup

Register Pixault services in `Program.cs`:

```csharp
builder.Services.AddPixault(options =>
{
    options.BaseUrl = "https://img.pixault.io";
    options.Project = "my-project";
    options.ApiKey = builder.Configuration["Pixault:ApiKey"];
});

builder.Services.AddPixaultBlazor();
```

Add the Radzen stylesheet and script to your `App.razor` or `_Host.cshtml`:

```html
<link rel="stylesheet" href="_content/Radzen.Blazor/css/material-base.css" />
<script src="_content/Radzen.Blazor/Radzen.Blazor.js"></script>
```

## Usage

### Upload Widget

```razor
<PixaultUploader Project="my-project"
                 MaxFiles="10"
                 MaxSizeMb="20"
                 OnUploadComplete="HandleUpload" />
```

### Image Gallery

```razor
<PixaultGallery Project="my-project"
                OnImageSelected="HandleImageSelected" />
```

### Image Detail

```razor
<PixaultImageDetail Project="my-project"
                    ImageId="@selectedImageId" />
```

### Image Component

```razor
<PixaultImage Project="tattoo" ImageId="img_01JKABC"
              Alt="Sunset tattoo" Quality="85"
              Widths="@(new[] { 400, 800, 1200 })"
              Sizes="(max-width: 800px) 100vw, 800px" />
```

Renders a `<picture>` element by default with AVIF/WebP/JPEG sources. Set `UsePicture="false"` for a single `<img>` tag with `srcset`.

## Dependencies

- [Pixault.Client](https://github.com/pixault/pixault-dotnet) — .NET SDK
- [Radzen.Blazor](https://blazor.radzen.com/) — UI components

## License

[MIT](LICENSE)
