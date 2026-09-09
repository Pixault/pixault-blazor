# Pixault.Blazor — Release Notes

## {ver} — 2026-09-09
- Consumes Pixault.Client 1.6.1, which fixes a NullReferenceException thrown out of
  PixaultUrlBuilder.Build() when an image id is null. A record with no image is an ordinary
  state, not a bug, and until 1.6.1 any such row crashed URL construction -- one took all 309
  barber.shop profile pages down on 2026-08-09. No API change in {pkg} itself.

## 1.7.0 — 2026-08-06
- Upgraded Radzen.Blazor 9.0.6 -> 11.2.0.
- Upgraded Microsoft.AspNetCore.Components.Web 10.0.3 -> 10.0.10.

## 1.6.0 — 2026-08-01
- Consumes Pixault.Client 1.6.0 (dual-mode URLs, f_auto default).
- `PublicId` plumbed through `UploadCompleteEventArgs`; main-image sites emit pretty publicId URLs (`PublicId ?? ImageId`), with thumbnails/previews/derived staying on legacy id URLs.
