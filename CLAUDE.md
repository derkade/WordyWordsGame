# WordyWordsGame — CLAUDE.md

## Project Overview
WordyWordsGame is a mobile crossword word puzzle game built in **Unity 6.3 LTS** using **2D URP**. Players swipe letters arranged in a circular wheel to form words that fill a crossword grid. The game features procedural puzzle generation, a coin economy, hint system, bonus word tracking with a word bank, and live dictionary lookups.

## Tech Stack
- **Engine:** Unity 6.3 LTS, 2D URP (Universal Render Pipeline)
- **Language:** C#
- **Input:** Unity Input System (pointer/touch via EventSystem interfaces)
- **Text:** TextMesh Pro (TMP)
- **Rendering:** Custom shaders (UIGlow, GlowTrail) with HDR bloom for glow effects
- **API:** Free Dictionary API (dictionaryapi.dev) for word definitions
- **Art:** 2D Adventure Background Pack

## Architecture

### Script Organization
```
Assets/Scripts/
├── Core/           — Game management, data, utilities
│   ├── GameManager.cs          — Main orchestrator, level loading, coin economy, UI flow
│   ├── LevelData.cs            — ScriptableObject defining a puzzle (letters, grid, word placements)
│   ├── WordPlacement.cs        — Serializable struct for word position/orientation on grid
│   ├── RuntimeLevelGenerator.cs — Procedural puzzle generation at runtime
│   ├── DictionaryService.cs    — Singleton fetching word definitions from API with caching
│   └── TweenHelper.cs          — Static coroutine-based animation utilities (scale, fade, shake, move)
├── Gameplay/       — Core gameplay mechanics
│   ├── CrosswordGrid.cs        — Builds/manages the crossword grid, reveals words, handles hints
│   ├── LetterWheel.cs          — Arranges letter tiles in a circle, handles shuffle
│   ├── LetterTile.cs           — Individual letter tile with selection state, static events
│   ├── SwipeController.cs      — Swipe input handling, word evaluation, event dispatching
│   ├── UISwipeLine.cs          — Draws glowing swipe trail between selected tiles
│   └── UIParticleEffect.cs     — UI-space particle bursts, sequences, and fireworks
├── UI/             — UI panels and helpers
│   ├── DefinitionPanel.cs      — Overlay showing word definitions fetched from DictionaryService
│   ├── WordBankClickHandler.cs — Detects clicks on TMP rich text links in word bank
│   └── LinedTextBackground.cs  — Procedural ruled-paper texture for text backgrounds
└── Editor/         — Editor-only tools (not in builds)
    ├── WordLevelGenerator.cs   — Editor window for batch-generating LevelData assets
    ├── SetupBloom.cs           — URP bloom configuration helper
    ├── FindMissingScripts.cs   — Utility to find missing script references
    └── ReorderHierarchy.cs     — Hierarchy organization tool
```

### Key Patterns
- **Event-driven communication:** SwipeController fires events (OnGridWordFound, OnExtraWordFound, OnInvalidWord, OnAlreadyFound) that GameManager subscribes to
- **Static tile events:** LetterTile uses static events so SwipeController doesn't need direct references to tiles
- **Object pooling:** UISwipeLine and UIParticleEffect pool their visual elements
- **Coroutine tweening:** TweenHelper provides PunchScale, ShakePosition, FadeTo, MoveTo, ScaleTo, ColorTo with easing curves (EaseOutQuad, EaseOutBack, EaseOutElastic)
- **ScriptableObject levels:** LevelData assets can be hand-crafted or auto-generated
- **Two-tier dictionary:** commonwords.txt (~4800 common words) for seed selection and grid placement; wordlist.txt (~10K words) for bonus/extra word validation

### Level Generation
- **Editor:** Tools > WordyWords > Level Generator (or Auto-Generate 3 Levels)
- **Runtime:** GameManager uses RuntimeLevelGenerator when `useRandomLevels = true`
- **Algorithm:** Pick a seed word (5-7 letters) → find sub-words formable from seed letters → build crossword via greedy placement with intersection scoring and compactness optimization → reject if <6 grid words or aspect ratio >2.5

### Crossword Placement Rules
- Words must intersect at least one existing word (matching letters)
- No parallel adjacency (perpendicular neighbors of non-intersection cells must be empty)
- Cells before start and after end of each word must be empty
- Scoring favors more intersections and compact bounding boxes

## Resources
- `Assets/Resources/commonwords.txt` — ~4800 common English words (uppercase, 3-7 letters)
- `Assets/Resources/wordlist.txt` — ~10K English words for full dictionary validation (frequency-filtered)
- `Assets/Levels/` — Hand-crafted levels (Level_1 through Level_3)
- `Assets/Levels/Generated/` — Auto-generated levels from editor tool

## Word List Filtering Tools
Python scripts in `tools/` for filtering word lists against the Free Dictionary API (dictionaryapi.dev).

### Pipeline (already completed once, results are the current word lists)
1. **API filter** — check every word against dictionaryapi.dev, keep only words that return HTTP 200
2. **Frequency filter** — cross-reference against OpenSubtitles top 20K (`tools/en_50k.txt`)
3. **Proper noun filter** — remove name-only words using `tools/first_names.txt`
4. **Profanity filter** — excluded to `tools/wordlist_spicy.txt`

### Scripts
- `tools/filter_wordlist.py` — filters `wordlist.txt` (async, 5 concurrent requests, resumable)
- `tools/filter_commonwords.py` — filters `commonwords.txt` (same approach)
- Both save progress to `tools/filter_progress.json` / `tools/filter_common_progress.json`
- Output goes to `*_filtered.txt`; manually rename to apply

### Re-running the filters
The current lists have ~15K words total. A fresh re-run takes ~1-2 hours at concurrency 5.
```
# Delete old progress so it checks fresh
rm tools/filter_progress.json tools/filter_common_progress.json
# Run (requires Python 3 + aiohttp: pip install aiohttp)
python tools/filter_commonwords.py
python tools/filter_wordlist.py
# Review output, then rename filtered files to apply
mv Assets/Resources/commonwords_filtered.txt Assets/Resources/commonwords.txt
mv Assets/Resources/wordlist_filtered.txt Assets/Resources/wordlist.txt
```

### Known issues
- The Free Dictionary API is inconsistent — some words return 200 on one run and 404 on another
- After max retries (8), words are kept by "benefit of the doubt" which can let bad words through
- Words like VER, SUR, SER slipped through the original run; manually removed 2026-03-03
- STEAD was missing from both lists; manually added 2026-03-03

## Prefabs
- `GridCell` — Single crossword grid cell (Image + TMP_Text child)
- `LetterTilePrefab` — Circular letter tile for the wheel (Image + TMP_Text + LetterTile component)

## Shaders
- `UIGlow.shader` — Additive glow blending for UI elements
- `GlowTrail.shader` — Glow effect for swipe trail lines

## Scene
- `WordyWords` — Single scene containing all gameplay (grid, wheel, UI panels)

## UI Flow
1. Level loads → grid builds, wheel populates with shuffled seed letters
2. Player swipes tiles → SwipeController evaluates word against grid/extra word sets
3. Grid word found → cells reveal with staggered punch animation + particles + coins
4. Bonus word found → flash counter + coins, word added to word bank
5. Word bank button → overlay with clickable TMP links for each found bonus word
6. Click bonus word → DefinitionPanel fetches and displays definition from API
7. All grid cells revealed → level complete panel with fireworks → next level
8. Hint button → reveals random unrevealed cell (costs coins)

## Important Notes
- Grid words come from commonwords.txt (well-known words players will recognize)
- Extra/bonus words validated against the full wordlist.txt (any valid English word earns bonus coins)
- DictionaryService caches results to avoid redundant API calls
- Letter wheel shuffle uses Fisher-Yates and only reassigns letters (positions stay fixed)
- The game currently uses a single scene; no scene transitions
- Canvas is set up for Screen Space rendering
- All animations are coroutine-based (no DOTween or animation controllers)

## Workflow Rules

### Commits
Do NOT commit or push unless the user explicitly asks. Wait for the user to say "commit", "push", etc. before staging, committing, or pushing.

When the user does ask to commit, commit messages should describe:
- What was built, changed, or fixed
- Key design decisions and why they were made
- Any known issues, TODOs, or next steps

Do NOT add co-author tags or AI attribution to commits. All commits should be authored solely by derkade.

### After Any Script Change
Always run `refresh_unity(compile="request")` followed by `read_console(types=["error"])` to verify compilation succeeds. Do not rely on the user switching to Unity to trigger recompilation. Always check the logs yourself.

### Before Committing
Always save the active scene via `manage_scene(action="save")` so any inspector changes are captured in the commit.

### MCP Usage
Minimize unnecessary MCP tool calls. Batch related operations when possible. Use `manage_scene` to read scene state once rather than making multiple small queries. Prefer editing script files directly over using MCP when possible — it's faster and uses fewer tokens.

### Debugging Stubborn Problems
If a problem is continually acting unresolved, decompose the problem into independent logical components. Validate each one individually. Then, synthesize your final answer from the validated pieces.

### Before Ending a Session
If context is running low, remind the user to commit and push before the session ends.

## Development Environment
- **Two development PCs:** PicoWaffen (RTX 4090) and CatDragon (Surface i7)
- **Version control:** GitHub (derkade / tforbrook@gmail.com)
- **Claude Code CLI** with Unity MCP integration (CoplayDev/unity-mcp)
- **MCP server:** http://127.0.0.1:8080/mcp
- **Python 3.13** + uv package manager required for MCP bridge

### Machine-Specific Paths
**CatDragon:**
- Python: `C:\Users\CatDragon\AppData\Local\Programs\Python\Python313`
- Project: `C:\Unity2D_Projects\WordyWordsGame`

**PicoWaffen:**
- Project: `C:\Unity2D_Projects\WordyWordsGame`
