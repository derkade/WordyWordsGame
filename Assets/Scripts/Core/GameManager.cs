using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Level Data")]
    [Tooltip("Array of LevelData assets defining each puzzle")]
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
    [Tooltip("Button that shuffles the letter wheel positions")]
    [SerializeField] private Button shuffleButton;
    [Tooltip("Button that reveals a random cell (costs coins)")]
    [SerializeField] private Button hintButton;
    [Tooltip("Background image that can change per level")]
    [SerializeField] private Image backgroundImage;

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

    private void Start()
    {
        coins = 100;
        currentLevelIndex = startLevelIndex;

        // Subscribe to swipe events
        swipeController.OnGridWordFound += HandleGridWordFound;
        swipeController.OnExtraWordFound += HandleExtraWordFound;
        swipeController.OnInvalidWord += HandleInvalidWord;
        swipeController.OnAlreadyFound += HandleAlreadyFound;

        // Button listeners
        shuffleButton.onClick.AddListener(OnShuffleClicked);
        hintButton.onClick.AddListener(OnHintClicked);
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

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
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("No levels assigned to GameManager!");
            return;
        }

        currentLevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        LevelData level = levels[currentLevelIndex];
        extraWordsFound = 0;

        // Set background
        if (backgroundImage != null)
        {
            if (level.backgroundSprite != null)
                backgroundImage.sprite = level.backgroundSprite;
            backgroundImage.color = level.backgroundColor;
        }

        // Build grid
        crosswordGrid.BuildGrid(level);

        // Build wheel
        letterWheel.BuildWheel(level.letters);

        // Set word sets for swipe controller
        swipeController.SetWordSets(level.GetGridWordSet(), level.GetExtraWordSet());

        // Update UI
        levelText.text = $"Level {currentLevelIndex + 1}";
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

    private void OnShuffleClicked()
    {
        letterWheel.ShuffleTiles();
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

        if (crosswordGrid.HintRevealCell())
        {
            AddCoins(-hintCost);

            // Check level complete after hint
            if (crosswordGrid.IsComplete())
            {
                StartCoroutine(ShowLevelComplete());
            }
        }
    }

    private void OnNextLevelClicked()
    {
        int nextIndex = currentLevelIndex + 1;
        if (nextIndex >= levels.Length)
            nextIndex = 0; // Loop back

        LoadLevel(nextIndex);
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
