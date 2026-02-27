using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LetterTile : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Image component used for the tile background color")]
    [SerializeField] private Image background;
    [Tooltip("TMP_Text component displaying the letter")]
    [SerializeField] private TMP_Text letterText;

    [Header("Colors")]
    [Tooltip("Background color when the tile is not selected")]
    [SerializeField] private Color normalColor = new Color(0.9f, 0.85f, 0.7f, 1f);
    [Tooltip("Background color when the tile is selected during a swipe")]
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.7f, 1f, 1f);

    public char Letter { get; private set; }
    public int WheelIndex { get; set; }
    public bool IsSelected { get; private set; }
    public RectTransform RectT { get; private set; }

    public static event Action<LetterTile> OnTilePointerDown;
    public static event Action<LetterTile> OnTilePointerEnter;
    public static event Action<PointerEventData> OnTileDrag;
    public static event Action<PointerEventData> OnTilePointerUp;

    private void Awake()
    {
        RectT = GetComponent<RectTransform>();
        if (background == null) background = GetComponent<Image>();
        if (letterText == null) letterText = GetComponentInChildren<TMP_Text>();
    }

    public void SetLetter(char c)
    {
        Letter = char.ToUpper(c);
        letterText.text = Letter.ToString();
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;
        background.color = selected ? selectedColor : normalColor;

        if (selected)
            StartCoroutine(TweenHelper.PunchScale(transform, Vector3.one * 0.15f, 0.2f));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnTilePointerDown?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Fire when pointer enters while dragging (eventData.dragging is true
        // because this class implements IDragHandler)
        if (eventData.dragging)
            OnTilePointerEnter?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Forward drag to SwipeController for raycast-based tile detection
        // and swipe line updates
        OnTileDrag?.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Forward pointer up to SwipeController for word evaluation
        OnTilePointerUp?.Invoke(eventData);
    }
}
