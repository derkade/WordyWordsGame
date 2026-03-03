using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SwipeController : MonoBehaviour, IPointerUpHandler, IPointerDownHandler, IDragHandler
{
    [Tooltip("Reference to the UISwipeLine that draws the swipe trail")]
    [SerializeField] private UISwipeLine swipeLine;
    [Tooltip("RectTransform of the wheel area used for screen-to-local coordinate conversion")]
    [SerializeField] private RectTransform wheelContainerRect;

    // Events fired after evaluating a swiped word
    public event Action<string> OnGridWordFound;
    public event Action<string> OnExtraWordFound;
    public event Action<string> OnInvalidWord;
    public event Action<string> OnAlreadyFound;

    private List<LetterTile> selectedTiles = new List<LetterTile>();
    private bool isSwiping;
    private HashSet<string> gridWords = new HashSet<string>();
    private HashSet<string> extraWords = new HashSet<string>();
    private HashSet<string> foundGridWords = new HashSet<string>();
    private HashSet<string> foundExtraWords = new HashSet<string>();
    private Canvas parentCanvas;
    private Camera canvasCamera;

    private void OnEnable()
    {
        LetterTile.OnTilePointerDown += HandleTilePointerDown;
        LetterTile.OnTilePointerEnter += HandleTilePointerEnter;
        LetterTile.OnTileDrag += HandleTileDrag;
        LetterTile.OnTilePointerUp += HandleTilePointerUp;
    }

    private void OnDisable()
    {
        LetterTile.OnTilePointerDown -= HandleTilePointerDown;
        LetterTile.OnTilePointerEnter -= HandleTilePointerEnter;
        LetterTile.OnTileDrag -= HandleTileDrag;
        LetterTile.OnTilePointerUp -= HandleTilePointerUp;
    }

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvasCamera = parentCanvas.worldCamera;
    }

    public void SetWordSets(HashSet<string> grid, HashSet<string> extra)
    {
        gridWords = grid;
        extraWords = extra;
        foundGridWords.Clear();
        foundExtraWords.Clear();
    }

    private void HandleTilePointerDown(LetterTile tile)
    {
        isSwiping = true;
        selectedTiles.Clear();
        AddTile(tile);
    }

    private void HandleTilePointerEnter(LetterTile tile)
    {
        if (!isSwiping) return;

        // Backtracking: if swiping back to second-to-last tile, deselect the last
        if (selectedTiles.Count >= 2 && tile == selectedTiles[selectedTiles.Count - 2])
        {
            var last = selectedTiles[selectedTiles.Count - 1];
            last.SetSelected(false);
            selectedTiles.RemoveAt(selectedTiles.Count - 1);
            UpdateSwipeLine();
            return;
        }

        // Don't add already-selected tiles
        if (tile.IsSelected) return;

        AddTile(tile);
    }

    private void AddTile(LetterTile tile)
    {
        tile.SetSelected(true);
        selectedTiles.Add(tile);
        UpdateSwipeLine();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Handled by LetterTile.OnTilePointerDown
    }

    public void OnDrag(PointerEventData eventData)
    {
        HandleDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        HandlePointerUp();
    }

    private void HandleTileDrag(PointerEventData eventData)
    {
        HandleDrag(eventData);
    }

    private void HandleTilePointerUp(PointerEventData eventData)
    {
        HandlePointerUp();
    }

    private void HandleDrag(PointerEventData eventData)
    {
        if (!isSwiping) return;

        // Check if pointer is over any unselected tile
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            var tile = result.gameObject.GetComponent<LetterTile>();
            if (tile != null)
            {
                HandleTilePointerEnter(tile);
                break;
            }
        }

        UpdateSwipeLineWithPointer(eventData);
    }

    private void HandlePointerUp()
    {
        if (!isSwiping) return;
        isSwiping = false;

        EvaluateWord();

        // Deselect all tiles
        foreach (var tile in selectedTiles)
            tile.SetSelected(false);
        selectedTiles.Clear();

        swipeLine.ClearLine();
    }

    private void EvaluateWord()
    {
        if (selectedTiles.Count < 2) return;

        string word = GetCurrentWord();

        if (gridWords.Contains(word))
        {
            if (foundGridWords.Contains(word))
                OnAlreadyFound?.Invoke(word);
            else
            {
                foundGridWords.Add(word);
                OnGridWordFound?.Invoke(word);
            }
        }
        else if (extraWords.Contains(word))
        {
            if (foundExtraWords.Contains(word))
                OnAlreadyFound?.Invoke(word);
            else
            {
                foundExtraWords.Add(word);
                OnExtraWordFound?.Invoke(word);
            }
        }
        else
        {
            OnInvalidWord?.Invoke(word);
        }
    }

    public string GetCurrentWord()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var tile in selectedTiles)
            sb.Append(tile.Letter);
        return sb.ToString();
    }

    private void UpdateSwipeLine()
    {
        if (selectedTiles.Count == 0) return;

        var positions = new List<RectTransform>();
        foreach (var tile in selectedTiles)
            positions.Add(tile.RectT);

        Vector2 pointerScreenPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheelContainerRect, pointerScreenPos, canvasCamera, out localPos);

        swipeLine.UpdateLine(positions, localPos);
    }

    private void UpdateSwipeLineWithPointer(PointerEventData eventData)
    {
        if (selectedTiles.Count == 0) return;

        var positions = new List<RectTransform>();
        foreach (var tile in selectedTiles)
            positions.Add(tile.RectT);

        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            wheelContainerRect, eventData.position, canvasCamera, out localPos);

        swipeLine.UpdateLine(positions, localPos);
    }

    public void MarkWordAsFound(string word)
    {
        string upper = word.ToUpper();
        if (gridWords.Contains(upper))
            foundGridWords.Add(upper);
    }

    public bool AllGridWordsFound => foundGridWords.Count >= gridWords.Count;
    public int FoundExtraWordCount => foundExtraWords.Count;
}
