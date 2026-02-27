using System;
using UnityEngine;

[Serializable]
public class WordPlacement
{
    public string word;
    public int row;
    public int startCol;
    public bool isHorizontal;

    public Vector2Int GetCellPosition(int letterIndex)
    {
        if (isHorizontal)
            return new Vector2Int(startCol + letterIndex, row);
        else
            return new Vector2Int(startCol, row + letterIndex);
    }
}
