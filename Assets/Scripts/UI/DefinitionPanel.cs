using System.Collections;
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

    private void Start()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(string word)
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        // Reset state
        if (wordTitle != null)
            wordTitle.text = word.ToUpper();
        if (phoneticText != null)
            phoneticText.text = "";
        if (definitionText != null)
            definitionText.text = "";
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            StartCoroutine(TweenHelper.FadeTo(canvasGroup, 1f, 0.2f));
        }

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
            if (!string.IsNullOrEmpty(meaning.partOfSpeech))
                sb.AppendLine($"<b><color=#AABBFF>{meaning.partOfSpeech}</color></b>");

            foreach (var def in meaning.definitions)
            {
                sb.AppendLine($"  {defNumber}. {def.definition}");
                if (!string.IsNullOrEmpty(def.example))
                    sb.AppendLine($"     <i><color=#999999>\"{def.example}\"</color></i>");
                defNumber++;
                if (defNumber > 12) break;
            }
            sb.AppendLine();
            if (defNumber > 12) break;
        }

        definitionText.text = sb.ToString().TrimEnd();
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