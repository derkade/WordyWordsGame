using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefinitionPanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("CanvasGroup for fade animation")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Displays the word being defined")]
    [SerializeField] private TMP_Text wordTitle;
    [Tooltip("Displays phonetic pronunciation")]
    [SerializeField] private TMP_Text phoneticText;
    [Tooltip("Displays formatted definitions")]
    [SerializeField] private TMP_Text definitionText;
    [Tooltip("Button to close the panel")]
    [SerializeField] private Button closeButton;
    [Tooltip("Loading indicator shown while fetching")]
    [SerializeField] private GameObject loadingIndicator;

    [Header("Navigation")]
    [Tooltip("Previous word button")]
    [SerializeField] private Button prevButton;
    [Tooltip("Next word button")]
    [SerializeField] private Button nextButton;
    [Tooltip("Displays current word index (e.g. 2/5)")]
    [SerializeField] private TMP_Text navCountText;
    [Tooltip("Navigation bar parent (hidden when only one word)")]
    [SerializeField] private GameObject navBar;
    [Tooltip("ScrollRect for the definition content")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Visual")]
    [Tooltip("Outer frame Image for rounded gray border")]
    [SerializeField] private Image outerFrame;
    [Tooltip("Inner panel Image for rounded black content area")]
    [SerializeField] private Image innerPanel;
    [Tooltip("Corner radius for rounded rectangles")]
    [SerializeField] private float cornerRadius = 24f;

    private List<string> wordList = new List<string>();
    private int currentWordIndex;
    private Scrollbar verticalScrollbar;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        if (prevButton != null)
            prevButton.onClick.AddListener(ShowPrevWord);
        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextWord);

        ApplyRoundedCorners();
        CreateScrollbar();
    }

    private void CreateScrollbar()
    {
        if (scrollRect == null) return;

        // Create scrollbar GameObject as sibling of scroll content area
        var scrollbarGO = new GameObject("VerticalScrollbar");
        scrollbarGO.transform.SetParent(scrollRect.transform.parent, false);

        // Position on right edge of the scroll area
        var scrollbarRT = scrollbarGO.AddComponent<RectTransform>();
        scrollbarRT.anchorMin = new Vector2(1f, 0f);
        scrollbarRT.anchorMax = new Vector2(1f, 1f);
        scrollbarRT.pivot = new Vector2(1f, 0.5f);
        scrollbarRT.sizeDelta = new Vector2(8f, 0f);
        scrollbarRT.anchoredPosition = Vector2.zero;

        // Track background (subtle)
        var trackImage = scrollbarGO.AddComponent<Image>();
        trackImage.color = new Color(1f, 1f, 1f, 0.05f);
        trackImage.raycastTarget = true;

        // Handle (sliding part)
        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(scrollbarGO.transform, false);
        var handleRT = handleGO.AddComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;
        var handleImage = handleGO.AddComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.3f);
        handleImage.raycastTarget = true;

        // Scrollbar component
        verticalScrollbar = scrollbarGO.AddComponent<Scrollbar>();
        verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
        verticalScrollbar.handleRect = handleRT;
        verticalScrollbar.targetGraphic = handleImage;

        // Hover color block for handle
        var cb = verticalScrollbar.colors;
        cb.normalColor = new Color(1f, 1f, 1f, 0.3f);
        cb.highlightedColor = new Color(1f, 1f, 1f, 0.5f);
        cb.pressedColor = new Color(1f, 1f, 1f, 0.6f);
        verticalScrollbar.colors = cb;

        // Wire to ScrollRect
        scrollRect.verticalScrollbar = verticalScrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = 2f;

        // Shrink scroll viewport to make room for scrollbar
        if (scrollRect.viewport != null)
        {
            scrollRect.viewport.offsetMax = new Vector2(-10f, scrollRect.viewport.offsetMax.y);
        }
    }

    private void ApplyRoundedCorners()
    {
        Sprite roundedSprite = GenerateRoundedRectSprite(128, 128, (int)cornerRadius);
        if (outerFrame != null)
        {
            outerFrame.sprite = roundedSprite;
            outerFrame.type = Image.Type.Sliced;
            outerFrame.pixelsPerUnitMultiplier = 1f;
        }
        if (innerPanel != null)
        {
            innerPanel.sprite = roundedSprite;
            innerPanel.type = Image.Type.Sliced;
            innerPanel.pixelsPerUnitMultiplier = 1f;
        }
    }

    private Sprite GenerateRoundedRectSprite(int width, int height, int radius)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color white = Color.white;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Find distance to nearest corner circle center
                float dx = 0f, dy = 0f;
                bool inCorner = false;

                if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inCorner = true; }
                else if (x >= width - radius && y < radius) { dx = x - (width - radius - 1); dy = radius - y; inCorner = true; }
                else if (x < radius && y >= height - radius) { dx = radius - x; dy = y - (height - radius - 1); inCorner = true; }
                else if (x >= width - radius && y >= height - radius) { dx = x - (width - radius - 1); dy = y - (height - radius - 1); inCorner = true; }

                if (inCorner)
                {
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius)
                        tex.SetPixel(x, y, clear);
                    else if (dist > radius - 1.5f)
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 1f - (dist - (radius - 1.5f)) / 1.5f));
                    else
                        tex.SetPixel(x, y, white);
                }
                else
                {
                    tex.SetPixel(x, y, white);
                }
            }
        }

        tex.Apply();

        // 9-slice border = radius on all sides
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    /// <summary>
    /// Show definition for a single word (no navigation).
    /// </summary>
    public void Show(string word)
    {
        Show(word, new List<string> { word }, 0);
    }

    /// <summary>
    /// Show definition with navigation through a list of words.
    /// </summary>
    public void Show(string word, List<string> words, int index)
    {
        wordList = words ?? new List<string> { word };
        currentWordIndex = Mathf.Clamp(index, 0, Mathf.Max(0, wordList.Count - 1));

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        LoadCurrentWord();

        // Show/hide nav bar
        UpdateNavBar();

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            StartCoroutine(TweenHelper.FadeTo(canvasGroup, 1f, 0.2f));
        }
    }

    private void LoadCurrentWord()
    {
        if (wordList.Count == 0) return;
        string word = wordList[currentWordIndex];

        // Reset state
        if (wordTitle != null)
            wordTitle.text = word.ToUpper();
        if (phoneticText != null)
            phoneticText.text = "";
        if (definitionText != null)
            definitionText.text = "";
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // Reset scroll to top
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 1f;

        // Fetch definition
        if (DictionaryService.Instance != null)
            DictionaryService.Instance.FetchDefinition(word, OnDefinitionReceived);
        else
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
            if (definitionText != null)
                definitionText.text = "<i>Dictionary service unavailable.</i>";
        }
    }

    private void UpdateNavBar()
    {
        bool showNav = wordList.Count > 1;
        if (navBar != null)
            navBar.SetActive(showNav);
        if (navCountText != null)
            navCountText.text = $"{currentWordIndex + 1}/{wordList.Count}";
        if (prevButton != null)
            prevButton.interactable = currentWordIndex > 0;
        if (nextButton != null)
            nextButton.interactable = currentWordIndex < wordList.Count - 1;
    }

    private void ShowPrevWord()
    {
        if (currentWordIndex > 0)
        {
            currentWordIndex--;
            LoadCurrentWord();
            UpdateNavBar();
        }
    }

    private void ShowNextWord()
    {
        if (currentWordIndex < wordList.Count - 1)
        {
            currentWordIndex++;
            LoadCurrentWord();
            UpdateNavBar();
        }
    }

    private void OnDefinitionReceived(DictionaryService.DictionaryResult result)
    {
        if (!gameObject.activeInHierarchy) return;

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);

        if (result.isError)
        {
            if (definitionText != null)
                definitionText.text = $"<i>{result.errorMessage}</i>";
            return;
        }

        // Phonetic
        if (phoneticText != null && !string.IsNullOrEmpty(result.phonetic))
            phoneticText.text = result.phonetic;

        // Build formatted definitions
        if (definitionText == null) return;

        var sb = new StringBuilder();
        int defNumber = 1;

        foreach (var meaning in result.meanings)
        {
            foreach (var def in meaning.definitions)
            {
                string pos = !string.IsNullOrEmpty(meaning.partOfSpeech)
                    ? $"<color=#AABBFF>({meaning.partOfSpeech})</color> "
                    : "";
                sb.AppendLine($"  {defNumber}. {pos}{def.definition}");
                if (!string.IsNullOrEmpty(def.example))
                    sb.AppendLine($"     <i><color=#999999>\"{def.example}\"</color></i>");
                sb.AppendLine();
                defNumber++;
                if (defNumber > 12) break;
            }
            if (defNumber > 12) break;
        }

        definitionText.text = sb.ToString().TrimEnd();
        StartCoroutine(RebuildScrollLayout());
    }

    private IEnumerator RebuildScrollLayout()
    {
        // Wait a frame for TMP to calculate text geometry
        yield return null;

        definitionText.ForceMeshUpdate();

        if (scrollRect != null && scrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            // Reset scroll position to top after content is sized
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void Hide()
    {
        StartCoroutine(HideCoroutine());
    }

    private IEnumerator HideCoroutine()
    {
        if (canvasGroup != null)
        {
            yield return TweenHelper.FadeTo(canvasGroup, 0f, 0.15f);
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }
}
