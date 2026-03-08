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
    [Tooltip("Border thickness around each cell in pixels")]
    [SerializeField] private float cellBorderWidth = 2f;
    [Tooltip("Padding inside the grid container in pixels")]
    [SerializeField] private float gridPadding = 2f;
    [Tooltip("Background color of unrevealed cells")]
    [SerializeField] private Color cellDefaultColor = new Color(1f, 1f, 1f, 0.8f);
    [Tooltip("Background color of revealed cells")]
    [SerializeField] private Color cellRevealedColor = new Color(0.3f, 0.3f, 0.5f, 1f);

    public void SetRevealedCellColor(Color color) { cellRevealedColor = color; }
    [Tooltip("Text color for revealed letters")]
    [SerializeField] private Color letterColor = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Corner radius for the outer cell border in pixels")]
    [SerializeField] private float cellCornerRadius = 6f;
    [Tooltip("Border color for each grid cell")]
    [SerializeField] private Color cellBorderColor = Color.black;

    [Header("Drop Shadow")]
    [Tooltip("Shadow color for grid cells")]
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
    [Tooltip("Shadow offset in pixels (negative Y = down)")]
    [SerializeField] private Vector2 shadowOffset = new Vector2(0f, -3f);
    [Tooltip("Shadow blur radius in pixels")]
    [SerializeField] private float shadowBlur = 5f;
    [Tooltip("Extra padding around shape for shadow rendering")]
    [SerializeField] private float shadowExpand = 6f;

    [Header("Inner Bevel (Unrevealed)")]
    [Tooltip("How deep the bevel extends from the edge in pixels for unrevealed cells")]
    [SerializeField] private float bevelSize = 24f;
    [Tooltip("Intensity of the bevel effect for unrevealed cells")]
    [Range(0f, 1f)]
    [SerializeField] private float bevelStrength = 0.5f;

    [Header("Inner Bevel (Revealed)")]
    [Tooltip("How deep the bevel extends from the edge in pixels for revealed cells")]
    [SerializeField] private float revealedBevelSize = 14f;
    [Tooltip("Intensity of the bevel effect for revealed cells")]
    [Range(0f, 1f)]
    [SerializeField] private float revealedBevelStrength = 0.25f;

    [Header("Gloss (Unrevealed)")]
    [Tooltip("Intensity of the glossy highlight on unrevealed cells")]
    [Range(0f, 1f)]
    [SerializeField] private float glossStrength = 0.35f;
    [Tooltip("How far down the gloss extends (0=top edge only, 1=full tile)")]
    [Range(0f, 1f)]
    [SerializeField] private float glossSize = 0.5f;
    [Tooltip("Curvature of the gloss inset (0=flat, higher=more curved)")]
    [Range(0f, 2f)]
    [SerializeField] private float glossCurve = 0.4f;

    [Header("Gloss (Revealed)")]
    [Range(0f, 1f)]
    [SerializeField] private float revealedGlossStrength = 0.2f;
    [Range(0f, 1f)]
    [SerializeField] private float revealedGlossSize = 0.5f;
    [Range(0f, 2f)]
    [SerializeField] private float revealedGlossCurve = 0.4f;

    [Header("Debug")]
    [Tooltip("Show all letters on the grid (cheat mode)")]
    [SerializeField] private bool cheatShowLetters = false;

    private class GridCell
    {
        public RectTransform rectTransform;
        public Image background;
        public TMP_Text letterText;
        public char letter;
        public bool isRevealed;
        public List<string> belongsToWords = new List<string>();
    }

    public event System.Action<string> OnGridWordClicked;

    private Dictionary<Vector2Int, GridCell> cells = new Dictionary<Vector2Int, GridCell>();
    private Dictionary<string, List<Vector2Int>> wordCellPositions = new Dictionary<string, List<Vector2Int>>();
    private Dictionary<string, bool> wordDirections = new Dictionary<string, bool>();
    private HashSet<string> revealedWords = new HashSet<string>();
    private LevelData currentLevel;
    private Camera canvasCamera;
    private Material roundedRectMaterial;
    private Material cellMaterial;
    private Material revealedCellMaterial;
    private float currentCellSize;

    private void Awake()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = canvas.worldCamera;
    }

    private void SetSharedMaterialProps(Material mat, float expandedSize, float shadowExp)
    {
        mat.SetVector("_RectSize", new Vector4(expandedSize, expandedSize, 0, 0));
        mat.SetFloat("_Radius", cellCornerRadius);
        mat.SetFloat("_BorderWidth", cellBorderWidth);
        mat.SetColor("_BorderColor", cellBorderColor);
        mat.SetColor("_ShadowColor", shadowColor);
        mat.SetVector("_ShadowOffset", new Vector4(shadowOffset.x, shadowOffset.y, 0, 0));
        mat.SetFloat("_ShadowBlur", shadowBlur);
        mat.SetFloat("_ShadowExpand", shadowExp);
    }

    private float ComputeCellSize(int gridWidth, int gridHeight)
    {
        // Force layout resolve so gridContainer.rect is accurate
        Canvas.ForceUpdateCanvases();

        Rect containerRect = gridContainer.rect;
        float shadowExp = Mathf.Max(shadowExpand, 0f);
        // Reserve space for shadow expand on outermost cells
        float availableWidth = containerRect.width - gridPadding * 2f - shadowExp * 2f;
        float availableHeight = containerRect.height - gridPadding * 2f - shadowExp * 2f;

        float fitByWidth = (availableWidth + cellSpacing) / gridWidth - cellSpacing;
        float fitByHeight = (availableHeight + cellSpacing) / gridHeight - cellSpacing;

        float size = Mathf.Min(fitByWidth, fitByHeight);
        return Mathf.Clamp(size, minCellSize, maxCellSize);
    }

    public void BuildGrid(LevelData levelData)
    {
        ClearGrid();
        currentLevel = levelData;

        // Auto-transpose if rotating 90° gives larger cells (better space usage)
        float normalSize = ComputeCellSize(levelData.gridWidth, levelData.gridHeight);
        float transposedSize = ComputeCellSize(levelData.gridHeight, levelData.gridWidth);
        if (transposedSize > normalSize * 1.05f)
        {
            int tmp = levelData.gridWidth;
            levelData.gridWidth = levelData.gridHeight;
            levelData.gridHeight = tmp;
            foreach (var wp in levelData.wordPlacements)
            {
                int oldRow = wp.row;
                wp.row = wp.startCol;
                wp.startCol = oldRow;
                wp.isHorizontal = !wp.isHorizontal;
            }
        }

        float cellSize = ComputeCellSize(levelData.gridWidth, levelData.gridHeight);
        currentCellSize = cellSize;

        // Create SDF rounded rect materials for this cell size
        if (roundedRectMaterial == null)
        {
            var shader = Shader.Find("UI/RoundedRect");
            if (shader != null)
                roundedRectMaterial = new Material(shader);
        }
        float shadowExp = Mathf.Max(shadowExpand, 0f);
        float expandedSize = cellSize + shadowExp * 2f;

        if (roundedRectMaterial != null)
        {
            // Unrevealed cell material (stronger bevel for white tiles)
            cellMaterial = new Material(roundedRectMaterial);
            SetSharedMaterialProps(cellMaterial, expandedSize, shadowExp);
            cellMaterial.SetFloat("_BevelSize", bevelSize);
            cellMaterial.SetFloat("_BevelStrength", bevelStrength);
            cellMaterial.SetFloat("_GlossStrength", glossStrength);
            cellMaterial.SetFloat("_GlossSize", glossSize);
            cellMaterial.SetFloat("_GlossCurve", glossCurve);

            // Revealed cell material (subtler bevel for dark tiles)
            revealedCellMaterial = new Material(roundedRectMaterial);
            SetSharedMaterialProps(revealedCellMaterial, expandedSize, shadowExp);
            revealedCellMaterial.SetFloat("_BevelSize", revealedBevelSize);
            revealedCellMaterial.SetFloat("_BevelStrength", revealedBevelStrength);
            revealedCellMaterial.SetFloat("_GlossStrength", revealedGlossStrength);
            revealedCellMaterial.SetFloat("_GlossSize", revealedGlossSize);
            revealedCellMaterial.SetFloat("_GlossCurve", revealedGlossCurve);
        }

        // Center the grid in the container
        float totalWidth = levelData.gridWidth * (cellSize + cellSpacing) - cellSpacing;
        float totalHeight = levelData.gridHeight * (cellSize + cellSpacing) - cellSpacing;

        foreach (var wp in levelData.wordPlacements)
        {
            string upperWord = wp.word.ToUpper();
            wordDirections[upperWord] = wp.isHorizontal;
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
                    rt.sizeDelta = new Vector2(expandedSize, expandedSize);

                    // Position: x goes right, y goes down (row 0 at top)
                    float xPos = pos.x * (cellSize + cellSpacing) - totalWidth * 0.5f + cellSize * 0.5f;
                    float yPos = -(pos.y * (cellSize + cellSpacing) - totalHeight * 0.5f + cellSize * 0.5f);
                    rt.anchoredPosition = new Vector2(xPos, yPos);

                    // Single image with SDF border support
                    Image bg = cellGO.GetComponent<Image>();
                    bg.color = cellDefaultColor;
                    if (cellMaterial != null)
                        bg.material = cellMaterial;

                    TMP_Text txt = cellGO.GetComponentInChildren<TMP_Text>();
                    txt.text = "";
                    txt.color = letterColor;
                    txt.fontSize = cellSize * 0.55f;

                    var clickHandler = cellGO.AddComponent<GridCellClickHandler>();
                    clickHandler.Init(this, pos);

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

        if (cheatShowLetters)
        {
            foreach (var kvp in cells)
            {
                var cell = kvp.Value;
                cell.letterText.text = cell.letter.ToString();
                cell.letterText.color = cell.isRevealed ? letterColor : new Color(0f, 0f, 0f, 0.7f);
            }
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
        wordDirections.Clear();
        revealedWords.Clear();
        pendingHintCells.Clear();
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
                if (revealedCellMaterial != null)
                    cell.background.material = revealedCellMaterial;

                // Staggered punch animation
                float delay = i * 0.08f;
                StartCoroutine(DelayedPunch(cell.rectTransform, delay));
            }
        }

        return true;
    }

    /// <summary>
    /// Marks a word as found in the revealed set without showing any letters.
    /// Used when flying tiles handle the per-cell reveal instead.
    /// </summary>
    public bool MarkWordRevealed(string word)
    {
        string upper = word.ToUpper();
        if (revealedWords.Contains(upper)) return false;
        if (!wordCellPositions.ContainsKey(upper)) return false;
        revealedWords.Add(upper);
        return true;
    }

    /// <summary>
    /// Visually reveals a single cell of a word by letter index.
    /// The cell shows its letter, changes color, and punches.
    /// </summary>
    public void RevealSingleCell(string word, int letterIndex)
    {
        string upper = word.ToUpper();
        if (!wordCellPositions.ContainsKey(upper)) return;

        var positions = wordCellPositions[upper];
        if (letterIndex < 0 || letterIndex >= positions.Count) return;

        var cell = cells[positions[letterIndex]];
        if (cell.isRevealed) return;

        cell.isRevealed = true;
        cell.letterText.text = cell.letter.ToString();
        cell.background.color = cellRevealedColor;
        if (revealedCellMaterial != null)
            cell.background.material = revealedCellMaterial;

        StartCoroutine(TweenHelper.PunchScale(cell.rectTransform, Vector3.one * 0.25f, 0.3f));
    }

    /// <summary>
    /// Returns the current computed cell size (for flying tile sizing).
    /// </summary>
    public float GetCellSize()
    {
        return currentCellSize;
    }

    /// <summary>
    /// Returns the unrevealed cell material (rounded rect with bevel) for flying tiles.
    /// </summary>
    public Material GetCellMaterial()
    {
        return cellMaterial;
    }

    public Color GetCellDefaultColor()
    {
        return cellDefaultColor;
    }

    private IEnumerator DelayedPunch(Transform target, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        yield return TweenHelper.PunchScale(target, Vector3.one * 0.2f, 0.3f);
    }

    /// <summary>
    /// Picks a random unrevealed cell and marks it logically revealed,
    /// but does NOT show the letter yet. Returns the cell's RectTransform
    /// so the caller can animate a streak to it, then call RevealHintCell().
    /// </summary>
    public List<string> HintRevealCell(out RectTransform revealedCellRT)
    {
        revealedCellRT = null;

        // Collect all unrevealed cells
        var unrevealed = new List<Vector2Int>();
        foreach (var kvp in cells)
        {
            if (!kvp.Value.isRevealed)
                unrevealed.Add(kvp.Key);
        }

        if (unrevealed.Count == 0) return null;

        // Pick a random unrevealed cell
        Vector2Int pos = unrevealed[Random.Range(0, unrevealed.Count)];
        var cell = cells[pos];
        cell.isRevealed = true;
        revealedCellRT = cell.rectTransform;
        pendingHintCells.Enqueue(cell);

        // Check if any word is now fully revealed
        var completedWords = new List<string>();
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
            {
                revealedWords.Add(wordName);
                completedWords.Add(wordName);
            }
        }

        return completedWords;
    }

    private Queue<GridCell> pendingHintCells = new Queue<GridCell>();

    /// <summary>
    /// Shows the letter and plays the reveal animation for the pending hint cell.
    /// Called by GameManager when the hint streak arrives.
    /// </summary>
    public void RevealHintCell()
    {
        if (pendingHintCells.Count == 0) return;

        var cell = pendingHintCells.Dequeue();
        cell.letterText.text = cell.letter.ToString();
        cell.background.color = cellRevealedColor;
        if (revealedCellMaterial != null)
            cell.background.material = revealedCellMaterial;
        StartCoroutine(TweenHelper.PunchScale(cell.rectTransform, Vector3.one * 0.3f, 0.4f));

        lastRevealedHintRT = cell.rectTransform;
    }

    private RectTransform lastRevealedHintRT;

    public RectTransform GetLastRevealedCellTransform()
    {
        return lastRevealedHintRT;
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

    public List<string> GetRevealedWords()
    {
        return new List<string>(revealedWords);
    }

    public void PunchWordCells(string word, float staggerDelay, List<bool> onlyThese = null)
    {
        string upper = word.ToUpper();
        if (!wordCellPositions.ContainsKey(upper)) return;
        var positions = wordCellPositions[upper];
        int punchIndex = 0;
        for (int i = 0; i < positions.Count; i++)
        {
            if (onlyThese != null && (i >= onlyThese.Count || !onlyThese[i]))
                continue;
            if (cells.ContainsKey(positions[i]))
                StartCoroutine(DelayedPunch(cells[positions[i]].rectTransform, punchIndex++ * staggerDelay));
        }
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

    /// <summary>
    /// Returns a bool per letter in the word — true if that cell was already revealed
    /// before this word was found (i.e. from a crossing word).
    /// </summary>
    public List<bool> GetWordCellRevealedStates(string word)
    {
        string upper = word.ToUpper();
        var result = new List<bool>();
        if (!wordCellPositions.ContainsKey(upper)) return result;

        foreach (var pos in wordCellPositions[upper])
        {
            if (cells.ContainsKey(pos))
                result.Add(cells[pos].isRevealed);
            else
                result.Add(false);
        }
        return result;
    }

    public void HandleCellClick(Vector2Int pos, Vector2 screenPos)
    {
        if (!cells.ContainsKey(pos)) return;
        var cell = cells[pos];
        if (!cell.isRevealed) return;

        // Only consider fully revealed words
        var words = new List<string>();
        foreach (string w in cell.belongsToWords)
        {
            if (revealedWords.Contains(w))
                words.Add(w);
        }
        if (words.Count == 0) return;

        string chosenWord;
        if (words.Count == 1)
        {
            chosenWord = words[0];
        }
        else
        {
            // Intersection: pick word based on click direction relative to cell center
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                cell.rectTransform, screenPos, canvasCamera, out Vector2 localPos);

            // Find horizontal and vertical words
            string horizontalWord = null;
            string verticalWord = null;
            foreach (string w in words)
            {
                if (wordDirections.ContainsKey(w))
                {
                    if (wordDirections[w])
                        horizontalWord = w;
                    else
                        verticalWord = w;
                }
            }

            if (Mathf.Abs(localPos.x) >= Mathf.Abs(localPos.y))
                chosenWord = horizontalWord ?? verticalWord ?? words[0];
            else
                chosenWord = verticalWord ?? horizontalWord ?? words[0];
        }

        OnGridWordClicked?.Invoke(chosenWord);
    }

}
