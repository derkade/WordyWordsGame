# Unity C# Conventions

## Script Organization
- **Core/** — Game management, data models, services, utilities
- **Gameplay/** — Interactive mechanics (grid, wheel, tiles, input, particles, coin streaks)
- **UI/** — Panels, overlays, click handlers, visual helpers
- **Editor/** — Editor-only tools (not included in builds)

## Coding Patterns

### Event-Driven Communication
Components communicate via C# events, not direct method calls. GameManager subscribes to SwipeController events. Never have child systems call GameManager directly.
```csharp
// Publisher
public event Action<string> OnGridWordFound;
// Subscriber (in Start or OnEnable)
swipeController.OnGridWordFound += HandleGridWordFound;
// Always unsubscribe in OnDestroy
```

### Static Events for Shared Types
When many instances of the same type need to broadcast (e.g., LetterTile), use static events so subscribers don't need references to every instance.
```csharp
public static event Action<LetterTile> OnTilePointerDown;
```

### Coroutine-Based Animation
All animations use TweenHelper static coroutines. Do NOT use DOTween, animation controllers, or Animator components for UI animations. Available methods:
- `TweenHelper.PunchScale` — bounce effect (e.g., coin text on arrival, cell reveal)
- `TweenHelper.ShakePosition` — error/rejection feedback
- `TweenHelper.FadeTo` — CanvasGroup alpha transitions (panel show/hide)
- `TweenHelper.MoveTo` — RectTransform position animation
- `TweenHelper.ScaleTo` — with EaseOutBack curve
- `TweenHelper.ColorTo` — Graphic color transitions
- `TweenHelper.DelayedAction` — deferred callbacks
- Easing helpers: `EaseOutQuad`, `EaseOutBack`, `EaseOutElastic`

### Object Pooling
Reuse GameObjects instead of instantiating/destroying per frame. Pooled systems:
- **UISwipeLine** — line segments between selected tiles
- **UIParticleEffect** — UI-space particle bursts
- **CoinStreakManager** — ribbon trail GameObjects (flat array + available stack)

Follow the pattern: pre-allocate in Awake, activate/deactivate as needed, never Destroy pooled objects.

### MaskableGraphic for Procedural UI Meshes
When you need procedural geometry in Canvas space (ribbons, trails, custom shapes), extend `MaskableGraphic` and override `OnPopulateMesh(VertexHelper vh)`. This is the UI-space equivalent of a MeshRenderer. Call `SetVerticesDirty()` each frame to trigger re-mesh. See CoinStreakTrail for the quad-strip ribbon pattern.

### ScriptableObject Data
Level definitions use ScriptableObject assets (LevelData). Keep game data in assets, not hardcoded. Use `[CreateAssetMenu]` for easy creation.

## SerializeField Conventions
- Always use `[SerializeField] private` — never public fields for Inspector exposure
- Add `[Header("Section")]` to group related fields
- Add `[Tooltip("...")]` to every serialized field explaining its purpose
- Order: References first, then settings/values
- Exception: expose properties via `public float Foo => foo;` when other scripts need read access (e.g., CoinStreakManager.TravelDuration)

## Naming
- PascalCase for classes, methods, properties, events
- camelCase for private fields and local variables
- Prefix events with `On` (e.g., `OnGridWordFound`)
- Prefix booleans with `is`/`has`/`can` (e.g., `isRevealed`)

## Canvas & UI
- Canvas is **Screen Space Camera** mode (NOT Overlay)
- Must pass `canvas.worldCamera` to `RectTransformUtility.ScreenPointToLocalPointInRectangle` — passing null gives wrong coordinates
- Use `CanvasGroup` for panel show/hide with fade
- Call `transform.SetAsLastSibling()` when showing overlay panels
- Set `interactable` and `blocksRaycasts` alongside alpha changes
- For right-aligned text, use `textBounds.center` (not `transform.position`) to get the visible text center

## Script Change Workflow
After creating or modifying any C# script:
1. Run `refresh_unity(compile="request")` to trigger recompilation
2. Run `read_console(types=["error"])` to verify no compilation errors
3. Only after clean compilation can new components/types be used in the scene

Before committing:
1. Save the active scene via `manage_scene(action="save")` to capture inspector changes

## Shaders
- `UIRoundedRect.shader` — SDF-based rounded corners for grid cells and buttons
- `UIGlow.shader` — additive blend (`SrcAlpha One`) with `_GlowIntensity` for coin streaks and fireworks
- `GlowTrail.shader` — glow effect for swipe trail lines
