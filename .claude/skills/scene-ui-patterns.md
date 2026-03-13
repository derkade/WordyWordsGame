# Scene & UI Patterns

## Scene Structure
Single scene: `SampleScene`. No scene transitions. All gameplay happens here.

## UI Panel Pattern
All overlay panels (level complete, word bank, definition) follow the same show/hide pattern:

### Showing a Panel
```csharp
panel.SetActive(true);
panel.transform.SetAsLastSibling();  // Render on top
if (canvasGroup != null)
{
    canvasGroup.alpha = 0f;
    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;
    StartCoroutine(TweenHelper.FadeTo(canvasGroup, 1f, 0.25f));
}
```

### Hiding a Panel
```csharp
if (canvasGroup != null)
{
    yield return TweenHelper.FadeTo(canvasGroup, 0f, 0.2f);
    canvasGroup.interactable = false;
    canvasGroup.blocksRaycasts = false;
}
panel.SetActive(false);
```

Always set `interactable` and `blocksRaycasts` together with alpha. Always use a coroutine for hiding so the fade completes before deactivation.

## SDF Rounded Rect Shader
Grid cells and buttons use `UIRoundedRect.shader` — vector-based rounded corners via signed distance field. Anti-aliased edges via `fwidth()`, pixel-perfect at any resolution.
- Properties: `_RectSize` (pixel dimensions), `_Radius` (corner px), `_Softness`
- Grid cells: shared border + fill materials with serialized corner radii on CrosswordGrid
- Buttons: per-button material instances

## Grid Cell Tap-to-Define
- `GridCellClickHandler` component added to each cell GO — implements `IPointerClickHandler`
- Click routes to `CrosswordGrid.HandleCellClick(cell, localPosition)`
- Intersection cells: disambiguates horizontal vs vertical by comparing `|localX|` vs `|localY|`
- **Only shows definitions for fully revealed words** (checks `revealedWords` set to prevent spoilers)
- GameManager subscribes to `OnGridWordClicked` event, opens DefinitionPanel
- Cheat mode: `cheatShowLetters` toggle shows all letters at 35% opacity for debugging

## Coin Streak Ribbon Trails
- **CoinStreakTrail** — `MaskableGraphic` subclass generating procedural quad strip mesh via `OnPopulateMesh`
- Ribbon follows cubic Bezier curve with 4 control points (P0/P1 at start, P2/P3 at destination)
- Width taper: `sin(frac * PI)` — eye/football shape (pointed both ends, fat middle)
- Head advances along Bezier, tail lags by `trailSpan`. Drain phase: tail catches up after head arrives
- Solid alpha with glow via `UI/Glow` shader (additive blend, `_GlowIntensity`)
- **CoinStreakManager** — pools trail GOs, creates glow material at runtime
- Random arc variants per trail: 1/3 upward, 1/3 mirrored downward, 1/3 mostly straight
- Targets `coinText.textBounds.center` for accurate right-aligned text positioning
- Machine gun arrival: each trail triggers particle burst + jackpot score climb on arrival

## Glow & Particle Effects
- Custom shaders: `UIGlow.shader` (additive blending for UI), `GlowTrail.shader` (swipe line glow)
- Glow textures are procedurally generated at runtime (not baked assets) — see `UISwipeLine.GenerateLineGlowSprite()` and `UIParticleEffect.GenerateSoftCircle()`
- Glow falloff uses power curve: `Mathf.Pow(1f - distFromCenter, glowFalloff)` with configurable falloff and brightness
- HDR bloom from URP post-processing amplifies the glow

## Particle Effects (UIParticleEffect)
UI-space particles, not world-space ParticleSystem. Everything is RectTransform-based with pooled Image components.
- `Play()` — burst from center of this object
- `PlayAt(RectTransform)` — burst at a target position (used for coin arrival machine gun bursts)
- `PlaySequence(List<RectTransform>, staggerDelay)` — staggered bursts along word cells
- `PlayFireworks(burstCount, duration)` — random position bursts with random colors (level complete)

## Swipe Line (UISwipeLine)
- Pooled line segments connecting selected tile centers
- Final segment extends from last tile to current pointer position
- Solid opaque line with dark outline for visibility against parallax backgrounds

## Parallax Background System
- Multiple themes: Jungle, Desert, Forest, Winter, etc. — randomly selected each level via `ApplyRandomTheme()`
- Assets at `Assets/Art/Backgrounds/{theme}/`
- Editor setup: `Tools/Setup All Parallax Themes`

## Grid Cell Prefab
- `GridCell.prefab` — needs Image (background) + child TMP_Text (letter display)
- Default state: colored background, empty text
- Revealed state: white background, letter shown, punch scale animation
- Cells at intersections belong to multiple words
- SDF rounded corners via UIRoundedRect shader

## Letter Tile Prefab
- `LetterTilePrefab.prefab` — needs Image (background) + child TMP_Text + LetterTile component
- Implements IPointerDownHandler, IPointerEnterHandler, IDragHandler, IPointerUpHandler
- Normal/selected color states
- Arranged in a circle by LetterWheel (radius configurable)

## DictionaryService
- Singleton pattern (`DictionaryService.Instance`)
- **Primary:** Free Dictionary API (`dictionaryapi.dev/api/v2/entries/en/{word}`)
- **Fallback:** Wiktionary REST API (`en.wiktionary.org/api/rest_v1/page/definition/{word}`) — tried on 404 from primary
- Wiktionary responses contain HTML tags — stripped via `StripHtml()` and common entity decoding
- Manual JSON parsing (no JsonUtility for root arrays)
- Results cached in memory dictionary
- 10 second timeout, graceful error handling for 404 and network failures

## Word Bank
- Found bonus words stored as `List<string>` in GameManager
- Displayed as TMP rich text with `<link>` tags for clickable words (gold, no underline)
- WordBankClickHandler detects TMP link clicks via `TMP_TextUtilities.FindIntersectingLink`
- Clicking a word opens DefinitionPanel
- LinedTextBackground generates ruled-paper lines behind text

## ScrollRect Gotchas
- Viewport needs transparent `Image` with `raycastTarget=true` — otherwise backdrop catches drags
- Content needs `VerticalLayoutGroup` so `ContentSizeFitter` can calculate height (without it, height=0, scroll snaps back)
- After setting text async, wait one frame then `ForceMeshUpdate()` + `LayoutRebuilder.ForceRebuildLayoutImmediate()`
- Use `RectMask2D` NOT `Mask` for clipping
