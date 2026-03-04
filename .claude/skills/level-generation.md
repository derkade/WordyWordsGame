# Level Generation

## Two-Tier Dictionary System
- **commonwords.txt** (~4,800 words) — curated common English words for seed selection and grid word placement. These are well-known words players will recognize.
- **wordlist.txt** (~10,400 words) — curated bonus words for extra/bonus word validation. Frequency-filtered against OpenSubtitles data, profanity removed, proper nouns removed. All have API definitions.

Both are in `Assets/Resources/` and loaded via `Resources.Load<TextAsset>()`. Words are uppercase, 3-7 letters, alphabetic only.

**Important:** Grid words come from commonwords.txt only. Bonus words come from wordlist.txt. The seed word is always from commonwords.txt.

## Generation Algorithm

### 1. Seed Selection
- Pick a random common word, 5-7 letters long
- Compute letter frequency counts for the seed
- Find all sub-words whose letters can be formed from the seed's letter counts:
  - Grid sub-words: from commonwords.txt
  - Bonus sub-words: from wordlist.txt

### 2. Quality Gate
- Seed must have at least 6 formable common sub-words to proceed
- If not, skip to next seed candidate
- Maximum 200 retry attempts before failing

### 3. Crossword Construction (Greedy)
- Place seed word first with random orientation
- For each remaining sub-word (sorted by length descending), find best placement position
- Stop when target grid words reached (default: 10)

### 4. Placement Scoring
Each candidate placement is scored by:
- **Intersections x 10** — more shared letters = better
- **Minus bounding box expansion** — penalizes sprawl, favors compact grids

### 5. Placement Validation Rules (CRITICAL)
These must ALL pass for a word to be placed:
- At least one intersection with existing grid (matching letter at shared position)
- No letter conflicts at intersection points
- No perpendicular adjacency for non-intersection cells (prevents parallel words touching)
- Cell before word start must be empty
- Cell after word end must be empty

### 6. Post-Construction Quality
- Reject if fewer than 6 words placed
- Reject if aspect ratio > 2.5 (too elongated)
- Normalize grid coordinates to start at (0,0)

### 7. Finalization
- Shuffle seed letters for wheel display (Fisher-Yates) so the word isn't immediately readable
- Extra words = all sub-words from wordlist.txt NOT placed on grid
- Random dark background color per level

## Two Generators
- **WordLevelGenerator** (Editor/) — batch generates LevelData .asset files via editor window. Menu: Tools > WordyWords > Level Generator
- **RuntimeLevelGenerator** (Core/) — generates LevelData at runtime when `GameManager.useRandomLevels = true`. Same algorithm, creates ScriptableObject instances in memory.

## WordPlacement Structure
```csharp
public string word;       // The word (uppercase)
public int row;           // Y position (0 = top)
public int startCol;      // X position of first letter
public bool isHorizontal; // true = left-to-right, false = top-to-bottom
```
`GetCellPosition(int letterIndex)` returns the Vector2Int grid position for any letter in the word.

## Coin Rewards
- Grid words: `coinsPerLetter (5) x word.Length` — 3-letter=15, 5-letter=25, 7-letter=35
- Bonus words: flat `coinsPerExtraWord (10)`
- Hint cost: 50 coins
