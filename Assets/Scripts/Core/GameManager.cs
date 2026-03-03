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

    [Header("Settings")]
    [Tooltip("Number of coins required to use a hint")]
    [SerializeField] private int hintCost = 50;
    [Tooltip("Coins awarded for finding a grid word")]
    [SerializeField] private int coinsPerWord = 25;
    [Tooltip("Coins awarded for finding a bonus word")]
    [SerializeField] private int coinsPerExtraWord = 10;

    private int currentLevelIndex;
    private int coins;
    private int extraWordsFound;
    private RuntimeLevelGenerator runtimeGenerator;
    private List<string> foundExtraWords = new List<string>();

    private void Start()
    {
        coins = 100;
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

    private void HandleGridWordFound(string word)
    {
        crosswordGrid.RevealWord(word);
        AddCoins(coinsPerWord);

        if (correctWordParticles != null)
        {
            var cellTransforms = crosswordGrid.GetWordCellTransforms(word);
            if (cellTransforms.Count > 0)
                correctWordParticles.PlaySequence(cellTransforms, 0.1f);
            else
                correctWordParticles.Play();
        }

        // Check level complete
        if (crosswordGrid.IsComplete())
        {
            StartCoroutine(ShowLevelComplete());
        }
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
            // Flash coin text red
            if (coinText != null)
                StartCoroutine(TweenHelper.ShakePosition(coinText.GetComponent<RectTransform>(), 5f, 0.3f));
            return;
        }

        var completedWords = crosswordGrid.HintRevealCell();
        if (completedWords != null)
        {
            AddCoins(-hintCost);

            // Fire effects for any words completed by this hint
            foreach (string word in completedWords)
            {
                swipeController.MarkWordAsFound(word);
                AddCoins(coinsPerWord);

                if (correctWordParticles != null)
                {
                    var cellTransforms = crosswordGrid.GetWordCellTransforms(word);
                    if (cellTransforms.Count > 0)
                        correctWordParticles.PlaySequence(cellTransforms, 0.1f);
                    else
                        correctWordParticles.Play();
                }
            }

            // Check level complete after hint
            if (crosswordGrid.IsComplete())
            {
                StartCoroutine(ShowLevelComplete());
            }
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
        if (coinText != null)
            coinText.text = coins.ToString();
    }

}
