using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Level Data")]
    [Tooltip("Generate random puzzles instead of using pre-baked levels")]
    [SerializeField] private bool useRandomLevels = true;
    [Tooltip("Array of LevelData assets defining each puzzle (used when random is off)")]
    [SerializeField] private LevelData[] levels;
    [Tooltip("Which level to load when the game starts")]
    [SerializeField] private int startLevelIndex = 0;

    [Header("Grid")]
    [Tooltip("Reference to the CrosswordGrid component that manages the puzzle grid")]
    [SerializeField] private CrosswordGrid crosswordGrid;

    [Header("Wheel")]
    [Tooltip("Reference to the LetterWheel component that arranges tiles in a circle")]
    [SerializeField] private LetterWheel letterWheel;
    [Tooltip("Reference to the SwipeController that handles swipe input and word evaluation")]
    [SerializeField] private SwipeController swipeController;
    [Tooltip("RectTransform of the WheelArea (auto-aligned horizontally to ButtonBar)")]
    [SerializeField] private RectTransform wheelAreaRT;
    [Tooltip("RectTransform of the ButtonBar (alignment reference for wheel)")]
    [SerializeField] private RectTransform buttonBarRT;

    [Header("UI")]
    [Tooltip("Displays the current level number")]
    [SerializeField] private TMP_Text levelText;
    [Tooltip("Displays the player's coin count")]
    [SerializeField] private TMP_Text coinText;
    [Tooltip("Displays the letters currently being swiped")]
    [SerializeField] private TMP_Text currentWordText;
    [Tooltip("Displays the count of bonus words found")]
    [SerializeField] private TMP_Text extraWordsCountText;
    [Tooltip("Button that reveals a random cell (costs coins)")]
    [SerializeField] private Button hintButton;
    [Tooltip("Background image that can change per level")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("Parallax background that randomly switches themes each level")]
    [SerializeField] private ParallaxBackground parallaxBackground;

    [Header("Word Bank")]
    [Tooltip("Button to open the Word Bank overlay")]
    [SerializeField] private Button wordBankButton;
    [Tooltip("Overlay panel showing found bonus words")]
    [SerializeField] private GameObject wordBankPanel;
    [Tooltip("CanvasGroup for fading the Word Bank panel")]
    [SerializeField] private CanvasGroup wordBankCG;
    [Tooltip("Text displaying clickable bonus words")]
    [SerializeField] private TMP_Text wordBankText;
    [Tooltip("Button to close the Word Bank overlay")]
    [SerializeField] private Button wordBankCloseButton;
    [Tooltip("Click handler on the Word Bank text for detecting word taps")]
    [SerializeField] private WordBankClickHandler wordBankClickHandler;

    [Header("Definition Panel")]
    [Tooltip("Overlay panel showing word definitions")]
    [SerializeField] private DefinitionPanel definitionPanel;

    [Header("Level Complete")]
    [Tooltip("Panel shown when all grid words are found")]
    [SerializeField] private GameObject levelCompletePanel;
    [Tooltip("CanvasGroup for fading the level complete panel in")]
    [SerializeField] private CanvasGroup levelCompleteCG;
    [Tooltip("Button to advance to the next level")]
    [SerializeField] private Button nextLevelButton;

    [Header("Particles")]
    [Tooltip("Particle burst played on grid cells when a word is found")]
    [SerializeField] private UIParticleEffect correctWordParticles;
    [Tooltip("Particle burst played when a bonus word is found")]
    [SerializeField] private UIParticleEffect bonusWordParticles;
    [Tooltip("Fireworks particle effect for level completion")]
    [SerializeField] private UIParticleEffect levelCompleteParticles;
    [Tooltip("Coin streak trails that fly from revealed cells to the score")]
    [SerializeField] private CoinStreakManager coinStreakManager;

    [Header("Settings")]
    [Tooltip("Number of coins required to use a hint")]
    [SerializeField] private int hintCost = 50;
    [Tooltip("Coins awarded per letter in a grid word (e.g. 5 × 4 letters = 20 coins)")]
    [SerializeField] private int coinsPerLetter = 5;
    [Tooltip("Coins awarded for finding a bonus word")]
    [SerializeField] private int coinsPerExtraWord = 10;
    [Tooltip("Coins awarded for collecting a cell coin")]
    [SerializeField] private int coinsPerCellCoin = 100;
    [Tooltip("Streak color for bonus word trails")]
    [SerializeField] private Color bonusStreakColor = new Color(0.3f, 0.6f, 1f, 1f);

    [Header("Grid Word Streaks")]
    [SerializeField] private bool gridStreaksEnabled = true;
    [SerializeField] private bool gridStreaksGlowOnTop = true;
    [SerializeField] private bool gridStreaksShowGlow = true;

    [Header("Bonus Word Streaks")]
    [SerializeField] private bool bonusStreakEnabled = true;
    [SerializeField] private bool bonusStreakGlowOnTop = true;
    [SerializeField] private bool bonusStreakShowGlow = true;

    [Header("Hint Streaks")]
    [SerializeField] private bool hintStreakEnabled = true;
    [SerializeField] private bool hintStreakGlowOnTop = true;
    [SerializeField] private bool hintStreakShowGlow = true;

    [Header("Persistence")]
    [Tooltip("Save/restore level and coins across sessions (enable for builds)")]
    [SerializeField] private bool persistProgress = false;
    [Tooltip("Clear saved progress (resets on next play)")]
    [SerializeField] private bool clearSaveOnNextPlay = false;

    [System.Serializable]
    private class IntPair
    {
        public int x, y;
        public IntPair(int x, int y) { this.x = x; this.y = y; }
    }

    [System.Serializable]
    private class SaveData
    {
        public int level;
        public int coins;
        public string letters;
        public int gridWidth;
        public int gridHeight;
        public List<WordPlacement> placements = new List<WordPlacement>();
        public List<string> extraWords = new List<string>();
        public List<string> revealedWords = new List<string>();
        public List<string> foundExtraWords = new List<string>();
        public int extraWordsFound;
        public List<IntPair> coinPositions = new List<IntPair>();
        public int themeIndex = -1;
    }

    private int currentLevelIndex;
    private int coins;
    private int displayedCoins;
    private Coroutine coinClimbCoroutine;
    private int extraWordsFound;
    private RuntimeLevelGenerator runtimeGenerator;
    private List<string> foundExtraWords = new List<string>();

    private void Start()
    {
        if (clearSaveOnNextPlay)
        {
            PlayerPrefs.DeleteKey("WW_Save");
            PlayerPrefs.Save();
            clearSaveOnNextPlay = false;
        }

        coins = 100;
        currentLevelIndex = startLevelIndex;
        displayedCoins = coins;

        if (useRandomLevels)
        {
            runtimeGenerator = new RuntimeLevelGenerator();
            runtimeGenerator.LoadDictionary();
        }

        // Subscribe to swipe events
        swipeController.OnGridWordFound += HandleGridWordFound;
        swipeController.OnExtraWordFound += HandleExtraWordFound;
        swipeController.OnInvalidWord += HandleInvalidWord;
        swipeController.OnAlreadyFound += HandleAlreadyFound;

        // Button listeners
        hintButton.onClick.AddListener(OnHintClicked);
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        // Word Bank buttons
        if (wordBankButton != null)
            wordBankButton.onClick.AddListener(OnWordBankClicked);
        if (wordBankCloseButton != null)
            wordBankCloseButton.onClick.AddListener(OnWordBankClosed);
        if (wordBankClickHandler != null)
            wordBankClickHandler.OnWordClicked += OnWordBankWordClicked;
        if (crosswordGrid != null)
        {
            crosswordGrid.OnGridWordClicked += OnGridWordClicked;
            crosswordGrid.OnCoinCollected += HandleCellCoinCollected;
        }
        if (wordBankPanel != null)
            wordBankPanel.SetActive(false);

        // Hide overlays
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        if (definitionPanel != null)
            definitionPanel.gameObject.SetActive(false);

        // Try to restore saved progress
        if (persistProgress && TryLoadSave())
        {
            UpdateCoinDisplay();
        }
        else
        {
            UpdateCoinDisplay();
            LoadLevel(currentLevelIndex);
        }

        // Align wheel center to button bar center after layout settles
        StartCoroutine(AlignWheelToButtons());
    }

    private IEnumerator AlignWheelToButtons()
    {
        yield return null; // wait one frame for layout
        if (wheelAreaRT != null && buttonBarRT != null)
        {
            Vector3 pos = wheelAreaRT.position;
            pos.x = buttonBarRT.position.x;
            wheelAreaRT.position = pos;
        }
    }

    private void OnDestroy()
    {
        if (swipeController != null)
        {
            swipeController.OnGridWordFound -= HandleGridWordFound;
            swipeController.OnExtraWordFound -= HandleExtraWordFound;
            swipeController.OnInvalidWord -= HandleInvalidWord;
            swipeController.OnAlreadyFound -= HandleAlreadyFound;
        }
        if (wordBankClickHandler != null)
            wordBankClickHandler.OnWordClicked -= OnWordBankWordClicked;
        if (crosswordGrid != null)
        {
            crosswordGrid.OnGridWordClicked -= OnGridWordClicked;
            crosswordGrid.OnCoinCollected -= HandleCellCoinCollected;
        }
    }

    private void Update()
    {
        // Update current word display from swipe controller
        if (currentWordText != null)
        {
            string word = swipeController.GetCurrentWord();
            currentWordText.text = word;
        }
    }

    public void LoadLevel(int index)
    {
        LevelData level;

        if (useRandomLevels && runtimeGenerator != null && runtimeGenerator.IsReady)
        {
            currentLevelIndex = index;
            level = runtimeGenerator.Generate();
            if (level == null)
            {
                Debug.LogError("Random level generation failed!");
                return;
            }
        }
        else
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("No levels assigned to GameManager!");
                return;
            }

            currentLevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
            level = levels[currentLevelIndex];
        }

        currentLoadedLevel = level;
        extraWordsFound = 0;
        foundExtraWords.Clear();

        // Set background
        if (backgroundImage != null)
        {
            if (level.backgroundSprite != null)
                backgroundImage.sprite = level.backgroundSprite;
            backgroundImage.color = level.backgroundColor;
        }

        // Switch parallax theme
        if (parallaxBackground != null)
        {
            parallaxBackground.ApplyRandomTheme();
            crosswordGrid.SetRevealedCellColor(parallaxBackground.ActiveRevealedCellColor);
        }

        // Build grid
        crosswordGrid.BuildGrid(level);

        // Build wheel
        letterWheel.BuildWheel(level.letters);

        // Set word sets for swipe controller
        swipeController.SetWordSets(level.GetGridWordSet(), level.GetExtraWordSet());

        // Update UI
        levelText.text = $"LEVEL {currentLevelIndex + 1}";
        if (extraWordsCountText != null)
        {
            extraWordsCountText.text = "";
            extraWordsCountText.alpha = 0f;
        }

        // Hide level complete panel
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        SaveProgress();
    }

    [Header("Flying Tiles")]
    [Tooltip("Time for each tile to fly from wheel to grid cell")]
    [SerializeField] private float tileFlightDuration = 0.5f;
    [Tooltip("Delay between each tile launch")]
    [SerializeField] private float tileLaunchStagger = 0.08f;

    private void HandleGridWordFound(string word)
    {
        // Mark word as found but don't reveal cells yet — flying tiles do that
        crosswordGrid.MarkWordRevealed(word);
        SaveProgress();
        StartCoroutine(WordRevealSequence(word));
    }

    [Tooltip("Delay between each cell reveal/punch in the stagger sequence")]
    [SerializeField] private float cellRevealStagger = 0.06f;

    private IEnumerator WordRevealSequence(string word)
    {
        var cellTransforms = crosswordGrid.GetWordCellTransforms(word);
        if (cellTransforms.Count == 0) yield break;

        // Snapshot which cells are already revealed (from crossing words)
        var alreadyRevealed = crosswordGrid.GetWordCellRevealedStates(word);

        Vector3 wheelCenter = letterWheel.transform.position;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
        Transform canvasTransform = canvas.transform;

        float cellSize = crosswordGrid.GetCellSize();

        Vector3 textCenter = (coinStreakManager != null && coinText != null)
            ? coinText.transform.TransformPoint(coinText.textBounds.center)
            : Vector3.zero;

        // Count unrevealed cells
        int unrevealedCount = 0;
        for (int i = 0; i < cellTransforms.Count; i++)
        {
            if (i >= alreadyRevealed.Count || !alreadyRevealed[i])
                unrevealedCount++;
        }

        // --- Phase 1: Launch flying tiles (purely visual, no reveal logic) ---
        var flyingTiles = new List<GameObject>();
        if (unrevealedCount > 0)
        {
            for (int i = 0; i < cellTransforms.Count; i++)
            {
                if (i < alreadyRevealed.Count && alreadyRevealed[i])
                    continue; // no tile needed for already-revealed cells

                var tile = CreateFlyingTile(canvasTransform, cellSize);
                tile.GetComponent<RectTransform>().position = wheelCenter;
                flyingTiles.Add(tile);

                // Fire-and-forget: all tiles launch simultaneously, land together
                int cellIdx = i;
                StartCoroutine(FlyTile(tile, wheelCenter, cellTransforms[cellIdx].position,
                    0f, tileFlightDuration, () =>
                    {
                        if (tile != null) Destroy(tile);
                    }));
            }

            // Wait for the first tile to visually arrive before starting reveals.
            // EaseOutBack reaches 0.99 at ~37% of duration.
            yield return new WaitForSeconds(tileFlightDuration * 0.37f);
        }

        // --- Phase 2: Unified stagger reveal for ALL cells ---
        // Every cell gets the same stagger treatment regardless of revealed state.
        for (int i = 0; i < cellTransforms.Count; i++)
        {
            // Reveal unrevealed cells
            if (i >= alreadyRevealed.Count || !alreadyRevealed[i])
                crosswordGrid.RevealSingleCell(word, i);

            // Punch every cell uniformly
            StartCoroutine(TweenHelper.PunchScale(cellTransforms[i], Vector3.one * 0.2f, 0.3f));

            // Stagger between cells
            if (i < cellTransforms.Count - 1)
                yield return new WaitForSeconds(cellRevealStagger);
        }

        // --- Phase 3: Particles and streaks after all cells revealed ---
        if (correctWordParticles != null)
            correctWordParticles.PlaySequence(cellTransforms, 0.05f);

        // Only award coins for newly revealed cells
        int coinsEarned = coinsPerLetter * unrevealedCount;

        if (gridStreaksEnabled && coinStreakManager != null && coinText != null && unrevealedCount > 0)
        {
            // Only streak from newly revealed cells
            var newCellTransforms = new List<RectTransform>();
            for (int i = 0; i < cellTransforms.Count; i++)
            {
                if (i >= alreadyRevealed.Count || !alreadyRevealed[i])
                    newCellTransforms.Add(cellTransforms[i]);
            }
            coinStreakManager.PlayStreaks(newCellTransforms, textCenter, gridStreaksGlowOnTop, gridStreaksShowGlow);
            float travelTime = coinStreakManager.TravelDuration;
            float stagger = coinStreakManager.StaggerDelay;
            for (int j = 0; j < newCellTransforms.Count; j++)
                StartCoroutine(CoinArrivalBurst(travelTime + j * stagger));
        }
        else if (coinsEarned > 0)
        {
            AddCoins(coinsEarned);
        }

        // Wait for streaks to finish before checking level complete
        if (coinStreakManager != null && unrevealedCount > 0)
        {
            float streakWait = coinStreakManager.TravelDuration
                + (unrevealedCount - 1) * coinStreakManager.StaggerDelay + 0.15f;
            yield return new WaitForSeconds(streakWait);
        }

        if (coinStreakManager == null || coinText == null)
            AddCoins(coinsEarned);

        if (crosswordGrid.IsComplete())
            StartCoroutine(ShowLevelComplete());
    }

    private GameObject CreateFlyingTile(Transform parent, float size)
    {
        var go = new GameObject("FlyingTile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = Vector3.one * 0.3f;

        var img = go.GetComponent<Image>();
        // Use the same rounded rect material and color as the grid cells, slightly transparent
        Color tileColor = crosswordGrid.GetCellDefaultColor();
        tileColor.a = 0.85f;
        img.color = tileColor;
        img.raycastTarget = false;

        Material cellMat = crosswordGrid.GetCellMaterial();
        if (cellMat != null)
            img.material = cellMat;

        go.SetActive(false);
        return go;
    }

    private IEnumerator FlyTile(GameObject tile, Vector3 fromWorld, Vector3 toWorld, float delay, float duration, System.Action onLand = null)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        tile.SetActive(true);
        tile.transform.SetAsLastSibling();
        var rt = tile.GetComponent<RectTransform>();
        rt.position = fromWorld;

        float elapsed = 0f;
        Vector3 startPos = fromWorld;
        Vector3 endPos = toWorld;
        Vector3 startScale = Vector3.one * 0.3f;
        Vector3 endScale = Vector3.one;
        bool landed = false;

        while (elapsed < duration)
        {
            if (tile == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // EaseOutBack for a snappy landing
            float ease = 1f + 2.70158f;
            float eased = 1f + (ease) * Mathf.Pow(t - 1f, 3f) + (ease - 1f) * Mathf.Pow(t - 1f, 2f);

            rt.position = Vector3.Lerp(startPos, endPos, eased);
            rt.localScale = Vector3.Lerp(startScale, endScale, eased);

            // Fire onLand the first frame the tile reaches the target
            if (!landed && eased >= 0.99f)
            {
                landed = true;
                onLand?.Invoke();
            }
            yield return null;
        }

        if (tile == null) yield break;
        rt.position = endPos;
        rt.localScale = endScale;
        if (!landed) onLand?.Invoke();
    }

    private void HandleExtraWordFound(string word)
    {
        extraWordsFound++;
        foundExtraWords.Add(word);
        AddCoins(coinsPerExtraWord);
        SaveProgress();

        if (bonusWordParticles != null && extraWordsCountText != null)
        {
            bonusWordParticles.transform.position = extraWordsCountText.transform.position;
            bonusWordParticles.Play();
        }

        if (extraWordsCountText != null)
            StartCoroutine(ShowExtraWordFlash());

        // Streak from bonus count to word bank button
        if (bonusStreakEnabled && coinStreakManager != null && extraWordsCountText != null && wordBankButton != null)
        {
            Vector3 fromPos = extraWordsCountText.transform.position;
            Vector3 toPos = wordBankButton.transform.position;
            coinStreakManager.PlaySingleStreak(fromPos, toPos, bonusStreakColor, bonusStreakGlowOnTop, bonusStreakShowGlow);
            StartCoroutine(WordBankArrivalBurst());
        }
    }

    private IEnumerator ShowExtraWordFlash()
    {
        // Show coin value earned at the counter position
        extraWordsCountText.text = $"+{coinsPerExtraWord}";
        extraWordsCountText.alpha = 1f;
        yield return TweenHelper.PunchScale(extraWordsCountText.transform, Vector3.one * 0.3f, 0.3f);
        yield return new WaitForSeconds(0.8f);
        // Fade out over 0.5s
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            extraWordsCountText.alpha = 1f - (elapsed / 0.5f);
            yield return null;
        }
        extraWordsCountText.alpha = 0f;
        extraWordsCountText.text = "";
    }

    private void HandleInvalidWord(string word)
    {
        // Shake the current word text
        if (currentWordText != null)
        {
            RectTransform rt = currentWordText.GetComponent<RectTransform>();
            StartCoroutine(TweenHelper.ShakePosition(rt, 8f, 0.3f));
        }
    }

    private void HandleAlreadyFound(string word)
    {
        // Gentle feedback - could flash the already-revealed cells
        if (currentWordText != null)
        {
            StartCoroutine(TweenHelper.PunchScale(currentWordText.transform, Vector3.one * 0.1f, 0.2f));
        }
    }

    private void OnHintClicked()
    {
        if (coins < hintCost)
        {
            if (coinText != null)
                StartCoroutine(TweenHelper.ShakePosition(coinText.GetComponent<RectTransform>(), 5f, 0.3f));
            return;
        }

        var completedWords = crosswordGrid.HintRevealCell(out RectTransform hintCellRT);
        if (completedWords != null)
        {
            AddCoins(-hintCost);

            // Fire a streak from the score to the hint cell
            if (hintStreakEnabled && coinStreakManager != null && hintCellRT != null && coinText != null)
            {
                Vector3 fromPos = coinText.transform.TransformPoint(coinText.textBounds.center);
                Vector3 toPos = hintCellRT.position;
                coinStreakManager.PlaySingleStreak(fromPos, toPos, null, hintStreakGlowOnTop, hintStreakShowGlow);

                // Delay the cell reveal and completed-word effects until streak arrives
                StartCoroutine(HintArrival(completedWords));
            }
            else
            {
                // No streak manager — reveal immediately
                crosswordGrid.RevealHintCell();
                HandleHintCompletedWords(completedWords);
            }
        }
    }

    private IEnumerator HintArrival(List<string> completedWords)
    {
        yield return new WaitForSeconds(coinStreakManager.TravelDuration);

        // Reveal the cell when the streak lands
        crosswordGrid.RevealHintCell();

        if (correctWordParticles != null)
        {
            // Burst on the revealed cell
            var cellRT = crosswordGrid.GetLastRevealedCellTransform();
            if (cellRT != null)
                correctWordParticles.PlayAt(cellRT);
        }

        HandleHintCompletedWords(completedWords);
    }

    private void HandleHintCompletedWords(List<string> completedWords)
    {
        foreach (string word in completedWords)
        {
            swipeController.MarkWordAsFound(word);
            AddCoins(coinsPerLetter * word.Length);

            if (correctWordParticles != null)
            {
                var cellTransforms = crosswordGrid.GetWordCellTransforms(word);
                if (cellTransforms.Count > 0)
                    correctWordParticles.PlaySequence(cellTransforms, 0.1f);
                else
                    correctWordParticles.Play();
            }
        }

        if (crosswordGrid.IsComplete())
        {
            StartCoroutine(ShowLevelComplete());
        }
    }

    private void OnNextLevelClicked()
    {
        if (levelCompleteParticles != null)
            levelCompleteParticles.Stop();
        if (definitionPanel != null)
            definitionPanel.gameObject.SetActive(false);

        if (useRandomLevels)
        {
            LoadLevel(currentLevelIndex + 1);
        }
        else
        {
            int nextIndex = currentLevelIndex + 1;
            if (nextIndex >= levels.Length)
                nextIndex = 0; // Loop back
            LoadLevel(nextIndex);
        }
    }

    private void OnWordBankWordClicked(string word)
    {
        if (definitionPanel != null)
        {
            int index = foundExtraWords.IndexOf(word);
            if (index < 0) index = 0;
            definitionPanel.Show(word, new List<string>(foundExtraWords), index);
        }
    }

    private void OnGridWordClicked(string word)
    {
        if (definitionPanel != null)
        {
            var revealedWords = crosswordGrid.GetRevealedWords();
            int index = revealedWords.IndexOf(word.ToUpper());
            if (index < 0) index = 0;
            definitionPanel.Show(word, revealedWords, index);
        }
    }

    private void OnWordBankClicked()
    {
        if (wordBankPanel == null) return;

        wordBankPanel.SetActive(true);

        if (wordBankText != null)
        {
            if (foundExtraWords.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < foundExtraWords.Count; i++)
                {
                    string w = foundExtraWords[i];
                    if (i > 0) sb.Append(",  ");
                    sb.Append($"<link=\"{w}\"><color=#FFD080>{w}</color></link>");
                }
                wordBankText.text = sb.ToString();
            }
            else
            {
                wordBankText.text = "No bonus words found yet.";
            }
            wordBankText.ForceMeshUpdate();
        }
        wordBankPanel.transform.SetAsLastSibling();
        if (wordBankCG != null)
        {
            wordBankCG.alpha = 0f;
            wordBankCG.interactable = true;
            wordBankCG.blocksRaycasts = true;
            StartCoroutine(TweenHelper.FadeTo(wordBankCG, 1f, 0.25f));
        }
    }

    private void OnWordBankClosed()
    {
        if (wordBankPanel == null) return;
        StartCoroutine(HideWordBank());
    }

    private IEnumerator HideWordBank()
    {
        if (wordBankCG != null)
        {
            yield return TweenHelper.FadeTo(wordBankCG, 0f, 0.2f);
            wordBankCG.interactable = false;
            wordBankCG.blocksRaycasts = false;
        }
        wordBankPanel.SetActive(false);
    }

    private IEnumerator ShowLevelComplete()
    {
        yield return new WaitForSeconds(0.5f);

        if (levelCompleteParticles != null)
            levelCompleteParticles.PlayFireworks(10, 3f);

        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
            // Move to last sibling so it renders on top of everything
            levelCompletePanel.transform.SetAsLastSibling();
            if (levelCompleteCG != null)
            {
                levelCompleteCG.alpha = 0f;
                levelCompleteCG.interactable = true;
                levelCompleteCG.blocksRaycasts = true;
                yield return TweenHelper.FadeTo(levelCompleteCG, 1f, 0.5f);
            }
        }
    }

    private void AddCoins(int amount)
    {
        coins += amount;
        if (coins < 0) coins = 0;
        UpdateCoinDisplay();
        SaveProgress();
    }

    private LevelData currentLoadedLevel;

    private void SaveProgress()
    {
        if (!persistProgress) return;
        if (currentLoadedLevel == null) return;

        var gridCoinPos = crosswordGrid.GetCoinPositions();
        var coinPairs = new List<IntPair>();
        foreach (var p in gridCoinPos)
            coinPairs.Add(new IntPair(p.x, p.y));

        var save = new SaveData
        {
            level = currentLevelIndex,
            coins = coins,
            letters = currentLoadedLevel.letters,
            gridWidth = currentLoadedLevel.gridWidth,
            gridHeight = currentLoadedLevel.gridHeight,
            placements = new List<WordPlacement>(currentLoadedLevel.wordPlacements),
            extraWords = new List<string>(currentLoadedLevel.extraWords),
            revealedWords = crosswordGrid.GetRevealedWords(),
            foundExtraWords = new List<string>(foundExtraWords),
            extraWordsFound = extraWordsFound,
            coinPositions = coinPairs,
            themeIndex = parallaxBackground != null ? parallaxBackground.ActiveThemeIndex : -1
        };

        PlayerPrefs.SetString("WW_Save", JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    private bool TryLoadSave()
    {
        string json = PlayerPrefs.GetString("WW_Save", "");
        if (string.IsNullOrEmpty(json)) return false;

        SaveData save;
        try { save = JsonUtility.FromJson<SaveData>(json); }
        catch { return false; }

        if (save == null || string.IsNullOrEmpty(save.letters)) return false;

        // Restore state
        currentLevelIndex = save.level;
        coins = save.coins;
        displayedCoins = coins;
        extraWordsFound = save.extraWordsFound;
        foundExtraWords = save.foundExtraWords ?? new List<string>();

        // Rebuild LevelData from save
        var level = ScriptableObject.CreateInstance<LevelData>();
        level.letters = save.letters;
        level.gridWidth = save.gridWidth;
        level.gridHeight = save.gridHeight;
        level.wordPlacements = save.placements;
        level.extraWords = save.extraWords;
        currentLoadedLevel = level;

        // Restore theme
        if (parallaxBackground != null)
        {
            if (save.themeIndex >= 0)
                parallaxBackground.ApplyTheme(save.themeIndex);
            else
                parallaxBackground.ApplyRandomTheme();
            crosswordGrid.SetRevealedCellColor(parallaxBackground.ActiveRevealedCellColor);
        }
        crosswordGrid.BuildGrid(level, skipCoins: true);
        letterWheel.BuildWheel(level.letters);
        swipeController.SetWordSets(level.GetGridWordSet(), level.GetExtraWordSet());
        levelText.text = $"LEVEL {currentLevelIndex + 1}";

        // Replay revealed words silently
        if (save.revealedWords != null)
        {
            foreach (string word in save.revealedWords)
            {
                crosswordGrid.RevealWord(word);
                swipeController.MarkWordAsFound(word);
            }
        }

        // Mark found extra words so they can't be re-submitted
        if (foundExtraWords != null)
        {
            foreach (string word in foundExtraWords)
                swipeController.MarkWordAsFound(word);
        }

        // Restore coin positions (after reveals, so collected coins on revealed cells are skipped)
        if (save.coinPositions != null && save.coinPositions.Count > 0)
        {
            var positions = new List<Vector2Int>();
            foreach (var p in save.coinPositions)
                positions.Add(new Vector2Int(p.x, p.y));
            crosswordGrid.PlaceCellCoinsAt(positions, crosswordGrid.GetCellSize());
        }

        // If puzzle was already complete, load a fresh next level instead
        if (crosswordGrid.IsComplete())
        {
            currentLevelIndex++;
            LoadLevel(currentLevelIndex);
        }

        if (extraWordsCountText != null)
        {
            extraWordsCountText.text = "";
            extraWordsCountText.alpha = 0f;
        }

        return true;
    }

    private void ClearSave()
    {
        if (!persistProgress) return;
        PlayerPrefs.DeleteKey("WW_Save");
        PlayerPrefs.Save();
    }

    private void UpdateCoinDisplay()
    {
        if (coinText == null) return;

        if (coinClimbCoroutine != null)
        {
            StopCoroutine(coinClimbCoroutine);
            // Snapshot what's currently displayed so next animation starts from there
            if (int.TryParse(coinText.text, out int current))
                displayedCoins = current;
        }
        coinClimbCoroutine = StartCoroutine(AnimateCoinDisplay());
    }

    private IEnumerator AnimateCoinDisplay()
    {
        int from = displayedCoins;
        int to = coins;
        if (from == to)
        {
            coinText.text = to.ToString();
            yield break;
        }

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Ease out — fast start, slow finish (jackpot feel)
            t = 1f - (1f - t) * (1f - t);
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
            coinText.text = current.ToString();
            yield return null;
        }

        coinText.text = to.ToString();
        displayedCoins = to;
        coinClimbCoroutine = null;
    }

    private IEnumerator WordBankArrivalBurst()
    {
        yield return new WaitForSeconds(coinStreakManager.TravelDuration);

        // Particle burst on word bank button
        if (bonusWordParticles != null && wordBankButton != null)
            bonusWordParticles.PlayAt(wordBankButton.GetComponent<RectTransform>());

        // Bounce the button and pop floating word count
        if (wordBankButton != null)
        {
            wordBankButton.transform.localScale = Vector3.one;
            StartCoroutine(TweenHelper.PunchScale(wordBankButton.transform, Vector3.one * 0.2f, 0.3f));
            StartCoroutine(FloatingCountPop(wordBankButton.GetComponent<RectTransform>()));
        }
    }

    private IEnumerator FloatingCountPop(RectTransform anchor)
    {
        // Parent to canvas root to avoid layout group interference
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        var go = new GameObject("FloatingCount", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(canvas.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"+{extraWordsFound}";
        tmp.fontSize = 48f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.5f, 0.8f, 1f, 1f);
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 180);
        tmp.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 60f);

        // Position in world space at the button, then animate upward
        Vector3 startWorld = anchor.position;
        float driftDistance = 120f * canvas.transform.lossyScale.y;

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            go.transform.position = startWorld + Vector3.up * driftDistance * TweenHelper.EaseOutQuad(t);
            // Hold full opacity for the first 40%, then fade out
            tmp.alpha = t < 0.4f ? 1f : 1f - ((t - 0.4f) / 0.6f);
            yield return null;
        }

        Destroy(go);
    }

    private IEnumerator CoinArrivalBurst(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Add coins for this single letter
        coins += coinsPerLetter;
        displayedCoins = coins;
        if (coinText != null)
            coinText.text = coins.ToString();

        // Particle burst on score
        if (correctWordParticles != null && coinText != null)
            correctWordParticles.PlayAt(coinText.rectTransform);

        // Bounce score on every arrival — reset scale first to prevent compounding
        if (coinText != null)
        {
            coinText.transform.localScale = Vector3.one;
            StartCoroutine(TweenHelper.PunchScale(coinText.transform, Vector3.one * 0.15f, 0.2f));
        }
    }

    private void HandleCellCoinCollected(RectTransform cellRT)
    {
        AddCoins(coinsPerCellCoin);

        // Streak from cell to score
        if (coinStreakManager != null && coinText != null)
        {
            Vector3 fromPos = cellRT.position;
            Vector3 toPos = coinText.transform.TransformPoint(coinText.textBounds.center);
            Color goldColor = new Color(1f, 0.84f, 0f, 1f);
            coinStreakManager.PlaySingleStreak(fromPos, toPos, goldColor, true, true);
        }

        // Floating +100 pop from the cell
        StartCoroutine(FloatingCoinPop(cellRT));
    }

    private IEnumerator FloatingCoinPop(RectTransform anchor)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindAnyObjectByType<Canvas>();

        var go = new GameObject("FloatingCoinPop", typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(canvas.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"+{coinsPerCellCoin}";
        tmp.fontSize = 48f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.84f, 0f, 1f);
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 180);
        tmp.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150f, 60f);

        Vector3 startWorld = anchor.position;
        float driftDistance = 120f * canvas.transform.lossyScale.y;

        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            go.transform.position = startWorld + Vector3.up * driftDistance * TweenHelper.EaseOutQuad(t);
            tmp.alpha = t < 0.4f ? 1f : 1f - ((t - 0.4f) / 0.6f);
            yield return null;
        }

        Destroy(go);
    }

}
