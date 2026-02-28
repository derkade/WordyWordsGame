using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Generates random LevelData puzzles at runtime.
/// Pure-logic extraction from the editor-only WordLevelGenerator.
/// </summary>
public class RuntimeLevelGenerator
{
    public int SeedMinLength { get; set; } = 5;
    public int SeedMaxLength { get; set; } = 7;
    public int MinSubWords { get; set; } = 6;
    public int TargetGridWords { get; set; } = 10;
    public int MaxRetries { get; set; } = 200;

    private List<string> commonWords;   // Common words used for seeds & grid placement
    private HashSet<string> fullDictionary; // Full dictionary used for bonus/extra words

    public void LoadDictionary()
    {
        commonWords = new List<string>();
        fullDictionary = new HashSet<string>();

        // Load common words (for seeds and grid words)
        var commonAsset = Resources.Load<TextAsset>("commonwords");
        if (commonAsset == null)
        {
            Debug.LogError("commonwords.txt not found in Resources!");
            return;
        }

        var seen = new HashSet<string>();
        foreach (var line in commonAsset.text.Split('\n'))
        {
            string w = line.Trim().ToUpper();
            if (w.Length >= 3 && w.Length <= 7 && w.All(char.IsLetter) && seen.Add(w))
                commonWords.Add(w);
        }

        // Load full dictionary (for extra/bonus word validation)
        var fullAsset = Resources.Load<TextAsset>("wordlist");
        if (fullAsset != null)
        {
            foreach (var line in fullAsset.text.Split('\n'))
            {
                string w = line.Trim().ToUpper();
                if (w.Length >= 3 && w.Length <= 7 && w.All(char.IsLetter))
                    fullDictionary.Add(w);
            }
        }

        Debug.Log($"[RuntimeLevelGenerator] Loaded {commonWords.Count} common words, {fullDictionary.Count} total dictionary words.");
    }

    public bool IsReady => commonWords != null && commonWords.Count >= 100;

    /// <summary>
    /// Generates a random LevelData instance. Returns null on failure.
    /// </summary>
    public LevelData Generate()
    {
        if (!IsReady)
        {
            Debug.LogError("[RuntimeLevelGenerator] Dictionary not loaded or too small.");
            return null;
        }

        // Shuffle seed candidates (from common words only)
        var seeds = commonWords
            .Where(w => w.Length >= SeedMinLength && w.Length <= SeedMaxLength)
            .OrderBy(_ => Random.value)
            .ToList();

        int retries = 0;
        foreach (var seed in seeds)
        {
            if (retries >= MaxRetries) break;
            retries++;

            // Find common sub-words for grid placement
            var commonSubWords = FindSubWords(seed, commonWords);
            if (commonSubWords.Count < 6) continue;

            // Sort by length descending, then random for variety
            commonSubWords.Sort((a, b) =>
            {
                int lenCmp = b.Length.CompareTo(a.Length);
                return lenCmp != 0 ? lenCmp : Random.Range(-1, 2);
            });

            // Include the seed itself as a placeable word
            var allWords = new List<string> { seed };
            allWords.AddRange(commonSubWords);

            var result = BuildCrossword(allWords);
            if (result == null) continue;

            var placements = result.Value.placements;
            var usedPositions = result.Value.usedPositions;

            if (placements.Count < 6) continue;

            // Quality: check aspect ratio
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            foreach (var pos in usedPositions.Keys)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }

            int w = maxX - minX + 1;
            int h = maxY - minY + 1;
            float aspect = (float)Mathf.Max(w, h) / Mathf.Max(1, Mathf.Min(w, h));
            if (aspect > 2.5f) continue;

            // Normalize to (0,0)
            if (minX != 0 || minY != 0)
            {
                foreach (var wp in placements)
                {
                    wp.startCol -= minX;
                    wp.row -= minY;
                }
            }

            // Build extra words list from FULL dictionary (any valid word earns bonus)
            var gridWords = new HashSet<string>(placements.Select(p => p.word));
            var allSubWords = FindSubWords(seed, fullDictionary);
            var extraWords = allSubWords.Where(sw => !gridWords.Contains(sw)).ToList();

            // Shuffle seed letters so the word isn't immediately readable on the wheel
            char[] shuffled = seed.ToCharArray();
            for (int s = shuffled.Length - 1; s > 0; s--)
            {
                int j = Random.Range(0, s + 1);
                (shuffled[s], shuffled[j]) = (shuffled[j], shuffled[s]);
            }

            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.letters = new string(shuffled);
            levelData.gridWidth = w;
            levelData.gridHeight = h;
            levelData.wordPlacements = placements;
            levelData.extraWords = extraWords;
            levelData.backgroundColor = new Color(
                Random.Range(0.1f, 0.2f),
                Random.Range(0.1f, 0.2f),
                Random.Range(0.2f, 0.35f),
                1f);

            return levelData;
        }

        Debug.LogWarning("[RuntimeLevelGenerator] Failed to generate a level after max retries.");
        return null;
    }

    // ─── Pure Logic (from WordLevelGenerator) ───

    private static Dictionary<char, int> LetterCounts(string word)
    {
        var counts = new Dictionary<char, int>();
        foreach (char c in word)
        {
            if (counts.ContainsKey(c)) counts[c]++;
            else counts[c] = 1;
        }
        return counts;
    }

    private static bool CanFormFrom(string candidate, Dictionary<char, int> seedCounts)
    {
        var needed = new Dictionary<char, int>();
        foreach (char c in candidate)
        {
            if (needed.ContainsKey(c)) needed[c]++;
            else needed[c] = 1;
        }

        foreach (var kvp in needed)
        {
            if (!seedCounts.TryGetValue(kvp.Key, out int available) || available < kvp.Value)
                return false;
        }
        return true;
    }

    private List<string> FindSubWords(string seed, IEnumerable<string> wordSource)
    {
        var seedCounts = LetterCounts(seed);
        var result = new List<string>();
        foreach (var word in wordSource)
        {
            if (word.Length <= seed.Length && word != seed && CanFormFrom(word, seedCounts))
                result.Add(word);
        }
        return result;
    }

    private struct CrosswordResult
    {
        public List<WordPlacement> placements;
        public Dictionary<Vector2Int, char> usedPositions;
    }

    private CrosswordResult? BuildCrossword(List<string> words)
    {
        var placements = new List<WordPlacement>();
        var usedPositions = new Dictionary<Vector2Int, char>();

        // Place first word with random orientation
        string first = words[0];
        bool firstHorizontal = Random.value > 0.5f;
        var firstWp = new WordPlacement
        {
            word = first,
            row = 0,
            startCol = 0,
            isHorizontal = firstHorizontal
        };
        placements.Add(firstWp);
        for (int i = 0; i < first.Length; i++)
            usedPositions[firstWp.GetCellPosition(i)] = first[i];

        // Try to place remaining words
        for (int wi = 1; wi < words.Count; wi++)
        {
            if (placements.Count >= TargetGridWords) break;

            string word = words[wi];
            var best = FindBestPlacement(word, usedPositions);
            if (best == null) continue;

            var wp = best;
            placements.Add(wp);
            for (int i = 0; i < word.Length; i++)
                usedPositions[wp.GetCellPosition(i)] = word[i];
        }

        if (placements.Count < 6) return null;

        return new CrosswordResult
        {
            placements = placements,
            usedPositions = usedPositions
        };
    }

    private WordPlacement FindBestPlacement(string word, Dictionary<Vector2Int, char> usedPositions)
    {
        WordPlacement bestWp = null;
        int bestScore = int.MinValue;

        // Compute current bounding box
        int curMinX = int.MaxValue, curMaxX = int.MinValue;
        int curMinY = int.MaxValue, curMaxY = int.MinValue;
        foreach (var pos in usedPositions.Keys)
        {
            if (pos.x < curMinX) curMinX = pos.x;
            if (pos.x > curMaxX) curMaxX = pos.x;
            if (pos.y < curMinY) curMinY = pos.y;
            if (pos.y > curMaxY) curMaxY = pos.y;
        }

        foreach (var kvp in usedPositions)
        {
            Vector2Int existingPos = kvp.Key;
            char existingChar = kvp.Value;

            for (int i = 0; i < word.Length; i++)
            {
                if (word[i] != existingChar) continue;

                // Try both orientations
                for (int orient = 0; orient < 2; orient++)
                {
                    bool horizontal = orient == 0;
                    var wp = new WordPlacement
                    {
                        word = word,
                        row = horizontal ? existingPos.y : existingPos.y - i,
                        startCol = horizontal ? existingPos.x - i : existingPos.x,
                        isHorizontal = horizontal
                    };

                    if (!CanPlace(wp, usedPositions)) continue;

                    // Score: intersections + compactness
                    int intersections = CountIntersections(wp, usedPositions);
                    int score = intersections * 10;

                    // Compactness: penalize bounding box expansion
                    int newMinX = curMinX, newMaxX = curMaxX;
                    int newMinY = curMinY, newMaxY = curMaxY;
                    for (int j = 0; j < word.Length; j++)
                    {
                        var p = wp.GetCellPosition(j);
                        if (p.x < newMinX) newMinX = p.x;
                        if (p.x > newMaxX) newMaxX = p.x;
                        if (p.y < newMinY) newMinY = p.y;
                        if (p.y > newMaxY) newMaxY = p.y;
                    }
                    int expansion = (newMaxX - newMinX + 1) * (newMaxY - newMinY + 1)
                                  - (curMaxX - curMinX + 1) * (curMaxY - curMinY + 1);
                    score -= expansion;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestWp = wp;
                    }
                }
            }
        }

        return bestWp;
    }

    private int CountIntersections(WordPlacement wp, Dictionary<Vector2Int, char> usedPositions)
    {
        int count = 0;
        for (int i = 0; i < wp.word.Length; i++)
        {
            Vector2Int pos = wp.GetCellPosition(i);
            if (usedPositions.ContainsKey(pos))
                count++;
        }
        return count;
    }

    private bool CanPlace(WordPlacement wp, Dictionary<Vector2Int, char> usedPositions)
    {
        bool hasIntersection = false;
        for (int i = 0; i < wp.word.Length; i++)
        {
            Vector2Int pos = wp.GetCellPosition(i);

            if (usedPositions.TryGetValue(pos, out char existing))
            {
                if (existing != wp.word[i])
                    return false; // Letter conflict
                hasIntersection = true;
            }
            else
            {
                // Check perpendicular adjacency (avoid parallel words touching)
                Vector2Int perpA, perpB;
                if (wp.isHorizontal)
                {
                    perpA = pos + Vector2Int.up;
                    perpB = pos + Vector2Int.down;
                }
                else
                {
                    perpA = pos + Vector2Int.left;
                    perpB = pos + Vector2Int.right;
                }

                if (usedPositions.ContainsKey(perpA) || usedPositions.ContainsKey(perpB))
                    return false;
            }
        }

        // Check cells before start and after end are empty
        Vector2Int before = wp.GetCellPosition(-1);
        Vector2Int after = wp.GetCellPosition(wp.word.Length);
        if (usedPositions.ContainsKey(before) || usedPositions.ContainsKey(after))
            return false;

        return hasIntersection;
    }
}
