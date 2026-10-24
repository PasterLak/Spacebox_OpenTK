# Interface fonts

Fonts that can be chosen in Options → Interface. Each folder holds the font file and its full license text.

Any other `.ttf` or `.otf` file put into this folder (or a subfolder) shows up in the Font list under its file name, at 12 px. Files with "pixel" in the name are drawn sharp (no smoothing), all others smooth.

| Font | File | Look | Languages | License | Source |
|---|---|---|---|---|---|
| Fusion Pixel 12px Monospaced (latin) 2026.10.16, **default** | `FusionPixel/FusionPixel12pxMonospacedLatin.ttf` | Pixel font on a 12 px grid | Latin, Cyrillic, Greek, plus CJK glyphs | SIL OFL 1.1 (includes parts of Ark Pixel, Cubic 11 and Galmuri, also OFL 1.1; all license texts in `FusionPixel/`) | https://github.com/TakWolf/fusion-pixel-font |
| Smooth (system font) | none, uses the operating system's default font | Smooth, anti-aliased, 12 px | depends on the system | the system font's own license, nothing is shipped | — |
| StarPixel | `StarPixel.ttf` | Used by the server dashboard | — | not documented | — |

## What the licenses allow

- **SIL OFL 1.1** (Fusion Pixel): free to use, bundle and ship inside a commercial game. The font file may not be sold on its own, the license texts must be shipped with it (`LICENSE-*.txt` in the font folder), and a modified version must not use the reserved font names.
- **MIT** (ProggyClean, part of Dear ImGui): free for any use, keep the copyright notice.

## Sizes

Pixel fonts look sharpest at their native pixel size or a whole multiple of it: Fusion Pixel 12 px (24, 36), ProggyClean 13 px.

Fusion Pixel is large (about 7 MB) because it contains CJK glyphs; only the Latin and Cyrillic ranges are loaded into the game atlas, so Chinese, Japanese and Korean can be added later by loading more glyph ranges.
