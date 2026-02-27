using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrosswordGrid : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent RectTransform where grid cells are spawned")]
    [SerializeField] private RectTransform gridContainer;
    [Tooltip("Prefab for each grid cell (needs Image + TMP_Text child)")]
    [SerializeField] private GameObject gridCellPrefab;

    [Header("Settings")]
    [Tooltip("Maximum cell size in pixels (used when grid is small)")]
    [SerializeField] private float maxCellSize = 120f;
    [Tooltip("Minimum cell size in pixels (floor for dense grids)")]
    [SerializeField] private float minCellSize = 30f;
    [Tooltip("Gap between adjacent grid cells in pixels")]
    [SerializeField] private float cellSpacing = 4f;
    [Tooltip("Padding inside the grid container in pixels")]
    [SerializeField] private float gridPadding = 2f;
    [Tooltip("Background color of unrevealed cells")]
    [SerializeField] private Color cellDefaultColor = new Color(0.3f, 0.3f, 0.5f, 1f);
    [Tooltip("Background color of revealed cells")]
    [SerializeField] private Color cellRevealedColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Text color for revealed letters")]
    [SerializeField] private Color letterColor = new Color(0.1f, 0.1f, 0.15f, 1f);

    private class GridCell
    {
        public RectTransform rectTransform;
        public Image background;
        public TMP_Text letterText;
        public char letter;
        public bool isRevealed;
        public List<string> belongsToWords = new List<string>();
    }

    private Dictionary<Vector2Int, GridCell> cells = new Dictionary<Vector2Int, GridCell>();
    private Dictionary<string, List<Vector2Int>> wordCellPositions = new Dictionary<string, List<Vector2Int>>();
    private HashSet<string> revealedWords = new HashSet<string>();
    private LevelData currentLevel;

    private float ComputeCellSize(int gridWidth, int gridHeight)
    {
        Rect containerRect = gridContainer.rect;
        float availableWidth = containerRect.width - gridPadding * 2f;
        float availableHeight = containerRect.height - gridPadding * 2f;

        float fitByWidth = (availableWidth + cellSpacing) / gridWidth - cellSpacing;
        float fitByHeight = (availableHeight + cellSpacing) / gridHeight - cellSpacing;

        float size = Mathf.Min(fitByWidth, fitByHeight);
        return Mathf.Clamp(size, minCellSize, maxCellSize);
    }

    public void BuildGrid(LevelData levelData)
    {
        ClearGrid();
        currentLevel = levelData;

        float cellSize = ComputeCellSize(levelData.gridWidth, levelData.gridHeight);

        // Center the grid in the container
        float totalWidth = levelData.gridWidth * (cellSize + cellSpacing) - cellSpacing;
        float totalHeight = levelData.gridHeight * (cellSize + cellSpacing) - cellSpacing;

        foreach (var wp in levelData.wordPlacements)
        {
            string upperWord = wp.word.ToUpper();
            var positions = new List<Vector2Int>();

            for (int i = 0; i < upperWord.Length; i++)
            {
                Vector2Int pos = wp.GetCellPosition(i);
                positions.Add(pos);

                if (cells.ContainsKey(pos))
                {
                    // Intersection: cell already exists, just add word reference
                    cells[pos].belongsToWords.Add(upperWord);
                }
                else
                {
                    // Create new cell
                    GameObject cellGO = Instantiate(gridCellPrefab, gridContainer);
                    cellGO.name = $"Cell_{pos.x}_{pos.y}";

                    RectTransform rt = cellGO.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(cellSize, cellSize);

                    // Position: x goes right, y goes down (row 0 at top)
                    float xPos = pos.x * (cellSize + cellSpacing) - totalWidth * 0.5f + cellSize * 0.5f;
                    float yPos = -(pos.y * (cellSize + cellSpacing) - totalHeight * 0.5f + cellSize * 0.5f);
                    rt.anchoredPosition = new Vector2(xPos, yPos);

                    Image bg = cellGO.GetComponent<Image>();
                    bg.color = cellDefaultColor;

                    TMP_Text txt = cellGO.GetComponentInChildren<TMP_Text>();
                    txt.text = "";
                    txt.color = letterColor;
                    txt.fontSize = cellSize * 0.55f;

                    var cell = new GridCell
                    {
                        rectTransform = rt,
                        background = bg,
                        letterText = txt,
                        letter = upperWord[i],
                        isRevealed = false,
                        belongsToWords = new List<string> { upperWord }
                    };

                    cells[pos] = cell;
                }
            }

            wordCellPositions[upperWord] = positions;
        }
    }

    public void ClearGrid()
    {
        foreach (var kvp in cells)
        {
            if (kvp.Value.rectTransform != null)
                Destroy(kvp.Value.rectTransform.gameObject);
        }
        cells.Clear();
        wordCellPositions.Clear();
        revealedWords.Clear();
    }

    public bool RevealWord(string word)
    {
        string upper = word.ToUpper();
        if (revealedWords.Contains(upper)) return false;
        if (!wordCellPositions.ContainsKey(upper)) return false;

        revealedWords.Add(upper);

        var positions = wordCellPositions[upper];
        for (int i = 0; i < positions.Count; i++)
        {
            var cell = cells[positions[i]];
            if (!cell.isRevealed)
            {
                cell.isRevealed = true;
                cell.letterText.text = cell.letter.ToString();
                cell.background.color = cellRevealedColor;

                // Staggered punch animation
                float delay = i * 0.08f;
                StartCoroutine(DelayedPunch(cell.rectTransform, delay));
            }
        }

        return true;
    }

    private IEnumerator DelayedPunch(Transform target, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        yield return TweenHelper.PunchScale(target, Vector3.one * 0.2f, 0.3f);
    }

    public bool HintRevealCell()
    {
        // Collect all unrevealed cells
        var unrevealed = new List<Vector2Int>();
        foreach (var kvp in cells)
        {
            if (!kvp.Value.isRevealed)
                unrevealed.Add(kvp.Key);
        }

        if (unrevealed.Count == 0) return false;

        // Pick a random unrevealed cell
        Vector2Int pos = unrevealed[Random.Range(0, unrevealed.Count)];
        var cell = cells[pos];
        cell.isRevealed = true;
        cell.letterText.text = cell.letter.ToString();
        cell.background.color = cellRevealedColor;
        StartCoroutine(TweenHelper.PunchScale(cell.rectTransform, Vector3.one * 0.3f, 0.4f));

        // Check if any word is now fully revealed
        foreach (string wordName in cell.belongsToWords)
        {
            if (revealedWords.Contains(wordName)) continue;

            bool allRevealed = true;
            foreach (var wp in wordCellPositions[wordName])
            {
                if (!cells[wp].isRevealed)
                {
                    allRevealed = false;
                    break;
                }
            }

            if (allRevealed)
                revealedWords.Add(wordName);
        }

        return true;
    }

    public bool IsComplete()
    {
        foreach (var kvp in cells)
        {
            if (!kvp.Value.isRevealed)
                return false;
        }
        return true;
    }

    public bool IsWordRevealed(string word)
    {
        return revealedWords.Contains(word.ToUpper());
    }

    public int TotalCells => cells.Count;

    public int RevealedCellCount
    {
        get
        {
            int count = 0;
            foreach (var kvp in cells)
            {
                if (kvp.Value.isRevealed)
                    count++;
            }
            return count;
        }
    }

    public List<RectTransform> GetWordCellTransforms(string word)
    {
        string upper = word.ToUpper();
        var result = new List<RectTransform>();
        if (!wordCellPositions.ContainsKey(upper)) return result;

        foreach (var pos in wordCellPositions[upper])
        {
            if (cells.ContainsKey(pos))
                result.Add(cells[pos].rectTransform);
        }
        return result;
    }
}
