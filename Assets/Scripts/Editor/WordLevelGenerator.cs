using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class WordLevelGenerator : EditorWindow
{
    private int levelCount = 3;
    private int seedMinLength = 5;
    private int seedMaxLength = 7;
    private int minSubWords = 12;
    private int targetGridWords = 10;
    private string outputFolder = "Assets/Levels/Generated";
    private int maxGlobalRetries = 200;
    private List<Sprite> backgroundSprites = new List<Sprite>();

    private List<string> dictionary;

    [MenuItem("Tools/WordyWords/Level Generator")]
    public static void ShowWindow()
    {
        GetWindow<WordLevelGenerator>("Level Generator");
    }

    [MenuItem("Tools/WordyWords/Auto-Generate 3 Levels")]
    public static void AutoGenerate()
    {
        var gen = CreateInstance<WordLevelGenerator>();
        gen.levelCount = 3;
        gen.GenerateLevels();
        DestroyImmediate(gen);
    }

    private void OnGUI()
    {
        GUILayout.Label("WordyWords Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        levelCount = EditorGUILayout.IntSlider("Level Count", levelCount, 1, 20);
        seedMinLength = EditorGUILayout.IntSlider("Seed Min Length", seedMinLength, 4, 6);
        seedMaxLength = EditorGUILayout.IntSlider("Seed Max Length", seedMaxLength, 5, 8);
        minSubWords = EditorGUILayout.IntSlider("Min Sub-Words Required", minSubWords, 8, 25);
        targetGridWords = EditorGUILayout.IntSlider("Target Grid Words", targetGridWords, 6, 14);
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        maxGlobalRetries = EditorGUILayout.IntSlider("Max Retries", maxGlobalRetries, 50, 500);

        EditorGUILayout.Space();
        GUILayout.Label("Background Sprites", EditorStyles.boldLabel);

        int newCount = EditorGUILayout.IntField("Count", backgroundSprites.Count);
        while (newCount > backgroundSprites.Count) backgroundSprites.Add(null);
        while (newCount < backgroundSprites.Count) backgroundSprites.RemoveAt(backgroundSprites.Count - 1);

        for (int i = 0; i < backgroundSprites.Count; i++)
        {
            backgroundSprites[i] = (Sprite)EditorGUILayout.ObjectField(
                $"Sprite {i}", backgroundSprites[i], typeof(Sprite), false);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Levels", GUILayout.Height(40)))
        {
            GenerateLevels();
        }
    }

    private void LoadDictionary()
    {
        dictionary = new List<string>();
        var asset = Resources.Load<TextAsset>("wordlist");
        if (asset == null)
        {
            Debug.LogError("wordlist.txt not found in Resources! Place it at Assets/Resources/wordlist.txt");
            return;
        }

        string[] lines = asset.text.Split('\n');
        var seen = new HashSet<string>();
        foreach (var line in lines)
        {
            string w = line.Trim().ToUpper();
            if (w.Length >= 3 && w.Length <= 7 && w.All(char.IsLetter) && seen.Add(w))
                dictionary.Add(w);
        }

        Debug.Log($"Loaded {dictionary.Count} words from dictionary.");
    }

    private void GenerateLevels()
    {
        LoadDictionary();
        if (dictionary.Count < 100)
        {
            EditorUtility.DisplayDialog("Error",
                $"Dictionary too small ({dictionary.Count} words). Need at least 100.", "OK");
            return;
        }

        EnsureFolderExists(outputFolder);
        int generated = 0;

        for (int level = 0; level < levelCount; level++)
        {
            EditorUtility.DisplayProgressBar("Generating Levels",
                $"Level {level + 1} of {levelCount}...", (float)level / levelCount);

            var levelData = GenerateSingleLevel(level);
            if (levelData != null)
            {
                string path = $"{outputFolder}/Level_{level + 1}.asset";
                AssetDatabase.CreateAsset(levelData, path);
                Debug.Log($"Created level: {path} — {levelData.wordPlacements.Count} grid words, " +
                          $"{levelData.extraWords.Count} extra words, seed: {levelData.letters}");
                generated++;
            }
            else
            {
                Debug.LogWarning($"Failed to generate level {level + 1} after {maxGlobalRetries} retries.");
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done",
            $"Generated {generated}/{levelCount} levels in {outputFolder}", "OK");
    }

    // ─── Phase A: Seed Selection ───

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

    private List<string> FindSubWords(string seed)
    {
        var seedCounts = LetterCounts(seed);
        var result = new List<string>();
        foreach (var word in dictionary)
        {
            if (word.Length <= seed.Length && word != seed && CanFormFrom(word, seedCounts))
                result.Add(word);
        }
        return result;
    }

    // ─── Phase B: Crossword Construction ───

    private struct Placement
    {
        public WordPlacement wp;
        public int score;
    }

    private LevelData GenerateSingleLevel(int levelIndex)
    {
        // Shuffle seed candidates
        var seeds = dictionary
            .Where(w => w.Length >= seedMinLength && w.Length <= seedMaxLength)
            .OrderBy(_ => Random.value)
            .ToList();

        int retries = 0;
        foreach (var seed in seeds)
        {
            if (retries >= maxGlobalRetries) break;
            retries++;

            var subWords = FindSubWords(seed);
            if (subWords.Count < minSubWords) continue;

            // Sort by length descending, then random for variety
            subWords.Sort((a, b) =>
            {
                int lenCmp = b.Length.CompareTo(a.Length);
                return lenCmp != 0 ? lenCmp : Random.Range(-1, 2);
            });

            // Include the seed itself as a placeable word
            var allWords = new List<string> { seed };
            allWords.AddRange(subWords);

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

            // Build extra words list (sub-words not placed on grid)
            var gridWords = new HashSet<string>(placements.Select(p => p.word));
            var extraWords = subWords.Where(sw => !gridWords.Contains(sw)).ToList();

            var levelData = ScriptableObject.CreateInstance<LevelData>();
            levelData.letters = seed; // preserves duplicate letters
            levelData.gridWidth = w;
            levelData.gridHeight = h;
            levelData.wordPlacements = placements;
            levelData.extraWords = extraWords;
            levelData.backgroundColor = new Color(
                Random.Range(0.1f, 0.2f),
                Random.Range(0.1f, 0.2f),
                Random.Range(0.2f, 0.35f),
                1f);

            if (backgroundSprites.Count > 0)
                levelData.backgroundSprite = backgroundSprites[levelIndex % backgroundSprites.Count];

            return levelData;
        }

        return null;
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
            if (placements.Count >= targetGridWords) break;

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

    private WordPlacement? FindBestPlacement(string word, Dictionary<Vector2Int, char> usedPositions)
    {
        WordPlacement? bestWp = null;
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

    // ─── Utilities ───

    private static void EnsureFolderExists(string path)
    {
        string[] parts = path.Replace("\\", "/").Split('/');
        string current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
