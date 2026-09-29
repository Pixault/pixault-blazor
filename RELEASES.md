# Pixault.Blazor — Release Notes

## 1.8.0 — 2026-09-29
- `PixaultGallery` gains `AllowBulkActions` (default true, so existing galleries are unchanged).
  Set it false when the gallery is a picker. The per-card tick boxes drive bulk Move/Delete and
  stop the click reaching the card's select handler, so in a chooser dialog they look like
  "choose this one" while actually arming a Delete and never raising `OnImageSelected`.
  With `AllowBulkActions="false"` the tick boxes, "select all" and the bulk bar are hidden, and
  clicking a card is the only selection gesture.
- List view now highlights the row raised through `OnImageSelected`, not only bulk-ticked rows.

## 1.7.1 — 2026-09-09
- Consumes Pixault.Client 1.6.1. That release fixes a NullReferenceException thrown out of
  PixaultUrlBuilder.Build() when an image id is null. A record with no image is an ordinary
  state rather than a bug. Until 1.6.1 any such row crashed URL construction -- one of them
  took all 309 barber.shop profile pages down on 2026-08-09. No API change in Pixault.Blazor itself.

## 1.7.0 — 2026-08-06
- Upgraded Radzen.Blazor 9.0.6 -> 11.2.0.
- Upgraded Microsoft.AspNetCore.Components.Web 10.0.3 -> 10.0.10.

## 1.6.0 — 2026-08-01
- Consumes Pixault.Client 1.6.0 (dual-mode URLs, f_auto default).
- `PublicId` plumbed through `UploadCompleteEventArgs`; main-image sites emit pretty publicId URLs (`PublicId ?? ImageId`), with thumbnails/previews/derived staying on legacy id URLs.
