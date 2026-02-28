using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class WordBankClickHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text textComponent;
    private Canvas parentCanvas;

    public event Action<string> OnWordClicked;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (textComponent == null) return;

        Camera cam = null;
        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = parentCanvas.worldCamera;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(
            textComponent, eventData.position, cam);

        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = textComponent.textInfo.linkInfo[linkIndex];
        string linkID = linkInfo.GetLinkID();

        if (!string.IsNullOrEmpty(linkID))
            OnWordClicked?.Invoke(linkID);
    }
}