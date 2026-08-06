# Pixault.Blazor — Release Notes

## 1.7.0 — 2026-08-06
- Upgraded Radzen.Blazor 9.0.6 -> 11.2.0.
- Upgraded Microsoft.AspNetCore.Components.Web 10.0.3 -> 10.0.10.

## 1.6.0 — 2026-08-01
- Consumes Pixault.Client 1.6.0 (dual-mode URLs, f_auto default).
- `PublicId` plumbed through `UploadCompleteEventArgs`; main-image sites emit pretty publicId URLs (`PublicId ?? ImageId`), with thumbnails/previews/derived staying on legacy id URLs.
