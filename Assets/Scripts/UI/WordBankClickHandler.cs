using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class WordBankClickHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text textComponent;
    private Canvas parentCanvas;
    private Camera cachedCamera;

    public event Action<string> OnWordClicked;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cachedCamera = parentCanvas.worldCamera;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (textComponent == null) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            textComponent, eventData.position, cachedCamera);

        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = textComponent.textInfo.linkInfo[linkIndex];
        string linkID = linkInfo.GetLinkID();

        if (!string.IsNullOrEmpty(linkID))
            OnWordClicked?.Invoke(linkID);
    }
}
