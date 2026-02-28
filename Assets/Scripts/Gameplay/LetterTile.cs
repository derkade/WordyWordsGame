using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class LetterTile : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Image component used for the tile background highlight")]
    [SerializeField] private Image background;
    [Tooltip("TMP_Text component displaying the letter")]
    [SerializeField] private TMP_Text letterText;

    [Header("Colors")]
    [Tooltip("Background color when the tile is not selected (transparent = no visible background)")]
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0f);
    [Tooltip("Background color when the tile is selected during a swipe")]
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.7f, 1f, 0.35f);
    [Tooltip("Color of the letter text")]
    [SerializeField] private Color letterColor = new Color(0.3f, 0.3f, 0.35f, 1f);

    private static Sprite circleSprite;

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

        // Use a circle sprite instead of the default square
        if (circleSprite == null)
            circleSprite = GenerateCircleSprite(128);
        background.sprite = circleSprite;
        background.type = Image.Type.Simple;
        background.color = normalColor;

        // Apply letter color
        if (letterText != null)
            letterText.color = letterColor;
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
        {
            transform.localScale = Vector3.one; // Reset to prevent stacking
            StartCoroutine(TweenHelper.PunchScale(transform, Vector3.one * 0.15f, 0.2f));
        }
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

    private static Sprite GenerateCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = size * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist + 0.5f); // anti-aliased edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
