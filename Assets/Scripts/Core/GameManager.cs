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
    [Tooltip("Streak color for bonus word trails")]
    [SerializeField] private Color bonusStreakColor = new Color(0.3f, 0.6f, 1f, 1f);

    private int currentLevelIndex;
    private int coins;
    private int displayedCoins;
    private Coroutine coinClimbCoroutine;
    private int extraWordsFound;
    private RuntimeLevelGenerator runtimeGenerator;
    private List<string> foundExtraWords = new List<string>();

    private void Start()
    {
        coins = 100;
        displayedCoins = 100;
        currentLevelIndex = startLevelIndex;

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
            crosswordGrid.OnGridWordClicked += OnGridWordClicked;
        if (wordBankPanel != null)
            wordBankPanel.SetActive(false);

        // Hide level complete panel
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        UpdateCoinDisplay();
        LoadLevel(currentLevelIndex);
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
            crosswordGrid.OnGridWordClicked -= OnGridWordClicked;
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
            parallaxBackground.ApplyRandomTheme();

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
        StartCoroutine(WordRevealSequence(word));
    }

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

        // Only fly tiles to unrevealed cells; last tile triggers streaks on landing
        int launchIndex = 0;
        int tilesLaunched = 0;
        bool streaksFired = false;

        // Count unrevealed cells first so we know which landing is the last
        int unrevearedCount = 0;
        for (int i = 0; i < cellTransforms.Count; i++)
        {
            if (i >= alreadyRevealed.Count || !alreadyRevealed[i])
                unrevearedCount++;
        }

        int landedCount = 0;
        for (int i = 0; i < cellTransforms.Count; i++)
        {
            int idx = i;

            // Skip cells already revealed by crossing words
            if (idx < alreadyRevealed.Count && alreadyRevealed[idx])
                continue;

            float delay = launchIndex * tileLaunchStagger;
            launchIndex++;
            tilesLaunched++;

            var tile = CreateFlyingTile(canvasTransform, cellSize);
            tile.GetComponent<RectTransform>().position = wheelCenter;
            StartCoroutine(FlyTile(tile, wheelCenter, cellTransforms[idx].position,
                delay, tileFlightDuration, () =>
            {
                crosswordGrid.RevealSingleCell(word, idx);
                if (tile != null) Destroy(tile);

                landedCount++;
                // Last tile landed — fire streaks from ALL cells immediately
                if (landedCount >= unrevearedCount && !streaksFired)
                {
                    streaksFired = true;

                    if (correctWordParticles != null)
                        correctWordParticles.PlaySequence(cellTransforms, 0.05f);

                    if (coinStreakManager != null && coinText != null)
                    {
                        coinStreakManager.PlayStreaks(cellTransforms, textCenter);
                        float travelTime = coinStreakManager.TravelDuration;
                        float stagger = coinStreakManager.StaggerDelay;
                        for (int j = 0; j < cellTransforms.Count; j++)
                            StartCoroutine(CoinArrivalBurst(travelTime + j * stagger));
                    }
                }
            }));
        }

        // If all cells were already revealed, fire streaks immediately
        if (tilesLaunched == 0)
        {
            if (correctWordParticles != null)
                correctWordParticles.PlaySequence(cellTransforms, 0.05f);

            if (coinStreakManager != null && coinText != null)
            {
                coinStreakManager.PlayStreaks(cellTransforms, textCenter);
                float travelTime = coinStreakManager.TravelDuration;
                float stagger = coinStreakManager.StaggerDelay;
                for (int i = 0; i < cellTransforms.Count; i++)
                    StartCoroutine(CoinArrivalBurst(travelTime + i * stagger));
            }
            else
            {
                AddCoins(coinsPerLetter * word.Length);
            }
        }

        // Wait for everything to finish before checking level complete
        float totalTime = (tilesLaunched > 0)
            ? (tilesLaunched - 1) * tileLaunchStagger + tileFlightDuration
            : 0f;
        if (coinStreakManager != null)
            totalTime += coinStreakManager.TravelDuration + (cellTransforms.Count - 1) * coinStreakManager.StaggerDelay + 0.15f;
        if (totalTime > 0f)
            yield return new WaitForSeconds(totalTime);

        if (coinStreakManager == null || coinText == null)
            AddCoins(coinsPerLetter * word.Length);

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
            yield return null;
        }

        if (tile == null) yield break;
        rt.position = endPos;
        rt.localScale = endScale;
        onLand?.Invoke();
    }

    private void HandleExtraWordFound(string word)
    {
        extraWordsFound++;
        foundExtraWords.Add(word);
        AddCoins(coinsPerExtraWord);

        if (bonusWordParticles != null && extraWordsCountText != null)
        {
            bonusWordParticles.transform.position = extraWordsCountText.transform.position;
            bonusWordParticles.Play();
        }

        if (extraWordsCountText != null)
            StartCoroutine(ShowExtraWordFlash());

        // Streak from bonus count to word bank button
        if (coinStreakManager != null && extraWordsCountText != null && wordBankButton != null)
        {
            Vector3 fromPos = extraWordsCountText.transform.position;
            Vector3 toPos = wordBankButton.transform.position;
            coinStreakManager.PlaySingleStreak(fromPos, toPos, bonusStreakColor);
            StartCoroutine(WordBankArrivalBurst());
        }
    }

    private IEnumerator ShowExtraWordFlash()
    {
        extraWordsCountText.text = $"+{extraWordsFound}";
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
            if (coinStreakManager != null && hintCellRT != null && coinText != null)
            {
                Vector3 fromPos = coinText.transform.TransformPoint(coinText.textBounds.center);
                Vector3 toPos = hintCellRT.position;
                coinStreakManager.PlaySingleStreak(fromPos, toPos);

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

        // Bounce the button
        if (wordBankButton != null)
        {
            wordBankButton.transform.localScale = Vector3.one;
            StartCoroutine(TweenHelper.PunchScale(wordBankButton.transform, Vector3.one * 0.2f, 0.3f));
        }
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

}
