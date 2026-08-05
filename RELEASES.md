# Pixault.Blazor — Release Notes

## 1.6.0 — 2026-08-01
- Consumes Pixault.Client 1.6.0 (dual-mode URLs, f_auto default).
- `PublicId` plumbed through `UploadCompleteEventArgs`; main-image sites emit pretty publicId URLs (`PublicId ?? ImageId`), with thumbnails/previews/derived staying on legacy id URLs.
