using UnityEngine;
using UnityEngine.EventSystems;

public class GridCellClickHandler : MonoBehaviour, IPointerClickHandler
{
    private CrosswordGrid parentGrid;
    private Vector2Int cellPosition;

    public void Init(CrosswordGrid grid, Vector2Int position)
    {
        parentGrid = grid;
        cellPosition = position;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (parentGrid != null)
            parentGrid.HandleCellClick(cellPosition, eventData.position);
    }
}
