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

    [Header("Scatterbomb")]
    [Tooltip("Button to trigger scatterbomb (reveals multiple cells)")]
    [SerializeField] private Button scatterbombButton;
    [Tooltip("Coin cost to use a scatterbomb")]
    [SerializeField] private int scatterbombCost = 200;
    [Tooltip("Number of cells revealed per scatterbomb")]
    [SerializeField] private int scatterbombCount = 5;

    [Header("Combo System")]
    [Tooltip("Time window to chain words for combo")]
    [SerializeField] private float comboTimeWindow = 8f;
    [Tooltip("Combo level that triggers the surge auto-reveal")]
    [SerializeField] private int maxComboLevel = 5;
    [Tooltip("Color of the combo ring at each level (index 0 = x1)")]
    [SerializeField] private Color[] comboColors = new Color[]
    {
        new Color(0.2f, 0.5f, 1f, 0.4f),   // x1: dim blue
        new Color(0.2f, 0.6f, 1f, 0.7f),   // x2: blue
        new Color(0.1f, 0.7f, 1f, 0.85f),  // x3: bright blue
        new Color(0.3f, 0.5f, 1f, 0.95f),  // x4: intense blue
        new Color(0.5f, 0.3f, 1f, 1f),     // x5: blue-purple (surge!)
    };

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

    // Combo system
    private int comboCount;
    private float comboTimer;
    private Image comboRingBase;  // solid opaque ring (visible against white)
    private Image comboRingGlow;  // additive glow ring on top
    private TMP_Text comboCountText;

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
        if (scatterbombButton != null)
            scatterbombButton.onClick.AddListener(OnScatterbombClicked);
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

        // Configure icon buttons (Hint + Bomb)
        ConfigureIconButtons();

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

        // Tick combo timer
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            float fill = Mathf.Clamp01(comboTimer / comboTimeWindow);
            if (comboRingBase != null) comboRingBase.fillAmount = fill;
            if (comboRingGlow != null) comboRingGlow.fillAmount = fill;
            if (comboTimer <= 0f)
                ResetCombo();
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

        // Setup combo ring around wheel
        SetupComboRing();

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
    private void HandleGridWordFound(string word)
    {
        IncrementCombo();
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

        // Only award coins for newly revealed cells (combo multiplier applies)
        int comboMult = Mathf.Max(comboCount, 1);
        int coinsEarned = coinsPerLetter * unrevealedCount * comboMult;

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
        IncrementCombo();
        extraWordsFound++;
        foundExtraWords.Add(word);
        AddCoins(coinsPerExtraWord * Mathf.Max(comboCount, 1));
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
        ResetCombo();

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

    private void OnScatterbombClicked()
    {
        if (coins < scatterbombCost)
        {
            if (coinText != null)
                StartCoroutine(TweenHelper.ShakePosition(coinText.GetComponent<RectTransform>(), 5f, 0.3f));
            return;
        }

        // Pick cells using the existing hint system (marks them revealed, queues for visual reveal)
        var cellRTs = new List<RectTransform>();
        var allCompletedWords = new List<string>();

        for (int i = 0; i < scatterbombCount; i++)
        {
            var completedWords = crosswordGrid.HintRevealCell(out RectTransform cellRT);
            if (completedWords == null) break;
            cellRTs.Add(cellRT);
            allCompletedWords.AddRange(completedWords);
        }

        if (cellRTs.Count == 0) return;

        AddCoins(-scatterbombCost);
        StartCoroutine(ScatterbombSequence(cellRTs, allCompletedWords));
    }

    private IEnumerator ScatterbombSequence(List<RectTransform> cellRTs, List<string> completedWords)
    {
        Vector3 scorePos = (coinText != null)
            ? coinText.transform.TransformPoint(coinText.textBounds.center)
            : Vector3.zero;
        Vector3 gridCenter = crosswordGrid.transform.position;

        // Phase 1: Streak from score to grid center (coins flying away)
        if (coinStreakManager != null && coinText != null)
        {
            Color goldColor = new Color(1f, 0.84f, 0f, 1f);
            coinStreakManager.PlaySingleStreak(scorePos, gridCenter, goldColor, true, true);
            yield return new WaitForSeconds(coinStreakManager.TravelDuration);
        }

        // Phase 2: Explosion at grid center + screen shake
        if (correctWordParticles != null)
        {
            var gridRT = crosswordGrid.GetComponent<RectTransform>();
            correctWordParticles.PlayAt(gridRT);
        }
        StartCoroutine(TweenHelper.ShakePosition(crosswordGrid.GetComponent<RectTransform>(), 12f, 0.35f));

        // Phase 3: Streaks radiate from center to each cell (slightly staggered)
        float outStagger = 0.06f;
        if (coinStreakManager != null)
        {
            for (int i = 0; i < cellRTs.Count; i++)
            {
                Color trailColor = new Color(1f, 0.7f, 0.2f, 1f);
                StartCoroutine(DelayedStreak(gridCenter, cellRTs[i].position, trailColor, i * outStagger));
            }
        }

        // Wait for last streak to arrive
        float travelTime = (coinStreakManager != null) ? coinStreakManager.TravelDuration : 0.3f;
        yield return new WaitForSeconds(travelTime + (cellRTs.Count - 1) * outStagger);

        // Phase 4: Reveal each cell with staggered punch + particles
        for (int i = 0; i < cellRTs.Count; i++)
        {
            crosswordGrid.RevealHintCell();

            if (correctWordParticles != null)
                correctWordParticles.PlayAt(cellRTs[i]);

            if (i < cellRTs.Count - 1)
                yield return new WaitForSeconds(0.08f);
        }

        // Handle any words completed by the scatterbomb
        foreach (string word in completedWords)
        {
            swipeController.MarkWordAsFound(word);
            AddCoins(coinsPerLetter * word.Length);
        }

        SaveProgress();

        if (crosswordGrid.IsComplete())
            StartCoroutine(ShowLevelComplete());
    }

    private IEnumerator DelayedStreak(Vector3 from, Vector3 to, Color color, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        coinStreakManager.PlaySingleStreak(from, to, color, true, true);
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
        SetupComboRing();
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

    // ---- Combo system ----

    private void SetupComboRing()
    {
        Transform wheelParent = letterWheel.transform;

        // Solid base ring (visible against white wheel)
        if (comboRingBase == null)
        {
            var baseGO = new GameObject("ComboRingBase", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            baseGO.transform.SetParent(wheelParent, false);

            comboRingBase = baseGO.GetComponent<Image>();
            comboRingBase.sprite = GenerateGlowRingSprite(256, 0.12f);
            comboRingBase.type = Image.Type.Filled;
            comboRingBase.fillMethod = Image.FillMethod.Radial360;
            comboRingBase.fillOrigin = (int)Image.Origin360.Top;
            comboRingBase.fillClockwise = true;
            comboRingBase.fillAmount = 0f;
            comboRingBase.raycastTarget = false;

            baseGO.transform.SetSiblingIndex(2);
        }

        // Additive glow ring underneath the base
        if (comboRingGlow == null)
        {
            var glowGO = new GameObject("ComboRingGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowGO.transform.SetParent(wheelParent, false);

            comboRingGlow = glowGO.GetComponent<Image>();
            comboRingGlow.sprite = GenerateGlowRingSprite(256, 0.12f);
            comboRingGlow.type = Image.Type.Filled;
            comboRingGlow.fillMethod = Image.FillMethod.Radial360;
            comboRingGlow.fillOrigin = (int)Image.Origin360.Top;
            comboRingGlow.fillClockwise = true;
            comboRingGlow.fillAmount = 0f;
            comboRingGlow.raycastTarget = false;

            var glowShader = Shader.Find("UI/Glow");
            if (glowShader != null)
            {
                var glowMat = new Material(glowShader);
                glowMat.SetFloat("_GlowIntensity", 3f);
                comboRingGlow.material = glowMat;
            }

            glowGO.transform.SetSiblingIndex(1);
        }

        if (comboCountText == null)
        {
            var textGO = new GameObject("ComboText", typeof(RectTransform), typeof(CanvasRenderer));
            textGO.transform.SetParent(wheelParent, false);

            comboCountText = textGO.AddComponent<TextMeshProUGUI>();
            comboCountText.fontSize = 40f;
            comboCountText.fontStyle = FontStyles.Bold;
            comboCountText.alignment = TextAlignmentOptions.Center;
            comboCountText.color = Color.white;
            comboCountText.outlineWidth = 0.3f;
            comboCountText.outlineColor = new Color32(0, 0, 0, 200);
            comboCountText.raycastTarget = false;
            comboCountText.text = "";
        }

        // Size both rings well beyond the wheel
        float ringSize = letterWheel.WheelBackgroundSize + 60f;
        comboRingBase.rectTransform.sizeDelta = new Vector2(ringSize, ringSize);
        comboRingBase.rectTransform.anchoredPosition = Vector2.zero;
        comboRingGlow.rectTransform.sizeDelta = new Vector2(ringSize, ringSize);
        comboRingGlow.rectTransform.anchoredPosition = Vector2.zero;

        // Position text above the wheel
        float textY = letterWheel.WheelBackgroundSize * 0.5f + 25f;
        comboCountText.rectTransform.sizeDelta = new Vector2(120f, 50f);
        comboCountText.rectTransform.anchoredPosition = new Vector2(0f, textY);

        ResetCombo();
    }

    private void IncrementCombo()
    {
        comboCount++;
        comboTimer = comboTimeWindow;

        {
            int colorIdx = Mathf.Clamp(comboCount - 1, 0, comboColors.Length - 1);
            Color ringColor = comboColors[colorIdx];
            if (comboRingBase != null)
            {
                comboRingBase.fillAmount = 1f;
                // Base is always fully opaque
                comboRingBase.color = new Color(ringColor.r, ringColor.g, ringColor.b, 1f);
            }
            if (comboRingGlow != null)
            {
                comboRingGlow.fillAmount = 1f;
                comboRingGlow.color = ringColor;
            }
        }

        if (comboCount >= 2 && comboCountText != null)
        {
            comboCountText.text = $"x{comboCount}";
            int colorIdx = Mathf.Clamp(comboCount - 1, 0, comboColors.Length - 1);
            Color txtColor = comboColors[colorIdx];
            txtColor.a = 1f;
            comboCountText.color = txtColor;
            comboCountText.transform.localScale = Vector3.one;
            StartCoroutine(TweenHelper.PunchScale(comboCountText.transform, Vector3.one * 0.4f, 0.3f));
        }

        // Pulse the wheel on each combo increment
        letterWheel.transform.localScale = Vector3.one;
        float pulseIntensity = 0.03f + comboCount * 0.02f;
        StartCoroutine(TweenHelper.PunchScale(letterWheel.transform, Vector3.one * pulseIntensity, 0.25f));

        if (comboCount >= maxComboLevel)
            StartCoroutine(ComboSurge());
    }

    private void ResetCombo()
    {
        comboCount = 0;
        comboTimer = 0f;
        if (comboRingBase != null) comboRingBase.fillAmount = 0f;
        if (comboRingGlow != null) comboRingGlow.fillAmount = 0f;
        if (comboCountText != null)
            comboCountText.text = "";
    }

    private IEnumerator ComboSurge()
    {
        // Brief dramatic pause
        yield return new WaitForSeconds(1.2f);

        string surgeWord = crosswordGrid.GetLongestUnrevealedWord();
        if (surgeWord == null)
        {
            ResetCombo();
            yield break;
        }

        // Big VFX: wheel scale pulse + grid shake
        letterWheel.transform.localScale = Vector3.one;
        StartCoroutine(TweenHelper.PunchScale(letterWheel.transform, Vector3.one * 0.2f, 0.5f));
        StartCoroutine(TweenHelper.ShakePosition(crosswordGrid.GetComponent<RectTransform>(), 15f, 0.4f));

        // Flash "SURGE!" text
        if (comboCountText != null)
        {
            comboCountText.text = "SURGE!";
            comboCountText.fontSize = 48f;
            comboCountText.color = new Color(0.5f, 0.3f, 1f, 1f);
            StartCoroutine(TweenHelper.PunchScale(comboCountText.transform, Vector3.one * 0.5f, 0.4f));
        }

        yield return new WaitForSeconds(0.6f);

        // Auto-reveal the longest unfound word
        swipeController.MarkWordAsFound(surgeWord);
        crosswordGrid.MarkWordRevealed(surgeWord);
        StartCoroutine(WordRevealSequence(surgeWord));

        // Reset combo text after surge VFX starts
        yield return new WaitForSeconds(0.5f);
        if (comboCountText != null)
            comboCountText.fontSize = 40f;
        ResetCombo();
    }

    private static Sprite GenerateGlowRingSprite(int size, float thicknessFraction)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        float outerR = size * 0.47f;
        float ringWidth = thicknessFraction * size;
        float ringCenter = outerR - ringWidth * 0.5f;
        float halfW = ringWidth * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Soft gaussian-like falloff from ring centerline
                float d = Mathf.Abs(dist - ringCenter) / halfW;
                float alpha = Mathf.Clamp01(1f - d);
                alpha *= alpha; // smooth quadratic falloff for soft glow

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ---- Icon button setup ----

    private void ConfigureIconButtons()
    {
        float iconBtnSize = 106f;

        // Hint button → square icon
        if (hintButton != null)
        {
            var rt = hintButton.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconBtnSize, iconBtnSize);

            var txt = hintButton.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.enabled = false;

            AddIconChild(hintButton.transform, GenerateLightbulbSprite(64), iconBtnSize * 0.55f,
                new Color(1f, 1f, 1f, 0.9f));
        }

        // Bomb button → square icon, placed between Hint and Word Bank
        if (scatterbombButton != null)
        {
            scatterbombButton.transform.SetSiblingIndex(1);

            var rt = scatterbombButton.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconBtnSize, iconBtnSize);

            var txt = scatterbombButton.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.enabled = false;

            AddIconChild(scatterbombButton.transform, GenerateStarburstSprite(64), iconBtnSize * 0.55f,
                new Color(1f, 1f, 1f, 0.9f));
        }
    }

    private static void AddIconChild(Transform parent, Sprite sprite, float size, Color color)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
    }

    private static Sprite GenerateLightbulbSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        float cx = size * 0.5f;
        float bulbCY = size * 0.6f;
        float bulbR = size * 0.26f;
        float lineW = size * 0.07f;

        // Filled bulb circle
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - bulbCY;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(bulbR - dist + 0.5f);
                if (alpha > 0f)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        // Neck connecting bulb to base
        float neckTop = bulbCY - bulbR + 2f;
        float neckBot = size * 0.25f;
        float neckHW = size * 0.1f;
        IconDrawLine(tex, size, cx - neckHW, neckTop, cx - neckHW, neckBot, lineW * 0.8f);
        IconDrawLine(tex, size, cx + neckHW, neckTop, cx + neckHW, neckBot, lineW * 0.8f);

        // Horizontal bars on the base (screw threads)
        for (int b = 0; b < 3; b++)
        {
            float by = neckBot + (neckTop - neckBot) * (b * 0.35f + 0.1f);
            IconDrawLine(tex, size, cx - neckHW, by, cx + neckHW, by, lineW * 0.7f);
        }

        // Light rays radiating upward from bulb
        float rayStart = bulbR + size * 0.03f;
        float rayEnd = bulbR + size * 0.13f;
        float[] angles = { 30f, 70f, 110f, 150f };
        foreach (float ang in angles)
        {
            float rad = ang * Mathf.Deg2Rad;
            IconDrawLine(tex, size,
                cx + Mathf.Cos(rad) * rayStart, bulbCY + Mathf.Sin(rad) * rayStart,
                cx + Mathf.Cos(rad) * rayEnd, bulbCY + Mathf.Sin(rad) * rayEnd,
                lineW);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static Sprite GenerateStarburstSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var clear = new Color(0, 0, 0, 0);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        float cx = size * 0.5f;
        float cy = size * 0.5f;
        float lineW = size * 0.09f;

        // 8 rays alternating long/short for starburst explosion look
        int rays = 8;
        for (int i = 0; i < rays; i++)
        {
            float angle = (i * 360f / rays + 22.5f) * Mathf.Deg2Rad;
            float r = (i % 2 == 0) ? size * 0.42f : size * 0.26f;
            IconDrawLine(tex, size, cx, cy,
                cx + Mathf.Cos(angle) * r,
                cy + Mathf.Sin(angle) * r,
                lineW);
        }

        // Small filled dot at center
        float dotR = size * 0.1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < dotR + 0.5f)
                {
                    float alpha = Mathf.Clamp01(dotR - dist + 0.5f);
                    Color existing = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(existing.a, alpha)));
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private static void IconDrawLine(Texture2D tex, int size, float x0, float y0, float x1, float y1, float width)
    {
        float halfW = width * 0.5f;
        int minX = Mathf.Max(0, (int)(Mathf.Min(x0, x1) - halfW - 1));
        int maxX = Mathf.Min(size - 1, (int)(Mathf.Max(x0, x1) + halfW + 1));
        int minY = Mathf.Max(0, (int)(Mathf.Min(y0, y1) - halfW - 1));
        int maxY = Mathf.Min(size - 1, (int)(Mathf.Max(y0, y1) + halfW + 1));

        float dx = x1 - x0, dy = y1 - y0;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.001f) return;

        for (int py = minY; py <= maxY; py++)
        {
            for (int px = minX; px <= maxX; px++)
            {
                float t = Mathf.Clamp01(((px - x0) * dx + (py - y0) * dy) / (len * len));
                float cx2 = x0 + t * dx, cy2 = y0 + t * dy;
                float dist = Mathf.Sqrt((px - cx2) * (px - cx2) + (py - cy2) * (py - cy2));
                float alpha = Mathf.Clamp01(halfW - dist + 0.5f);
                if (alpha > 0f)
                {
                    Color existing = tex.GetPixel(px, py);
                    float blended = Mathf.Max(existing.a, alpha);
                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, blended));
                }
            }
        }
    }

}
