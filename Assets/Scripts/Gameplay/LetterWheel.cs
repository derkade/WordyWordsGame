using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LetterWheel : MonoBehaviour
{
    [Tooltip("Parent RectTransform where letter tiles are spawned in a circle")]
    [SerializeField] private RectTransform wheelContainer;
    [Tooltip("Prefab for each letter tile (needs LetterTile component)")]
    [SerializeField] private GameObject letterTilePrefab;
    [Tooltip("Radius of the circular tile arrangement in pixels")]
    [SerializeField] private float wheelRadius = 140f;

    private List<LetterTile> tiles = new List<LetterTile>();
    private string currentLetters;

    public void BuildWheel(string letters)
    {
        ClearWheel();
        currentLetters = letters.ToUpper();

        int count = currentLetters.Length;
        float angleStep = 360f / count;
        // Start from top (-90 deg) so first letter is at top
        float startAngle = 90f;

        // Compute tile size: use arc gap between tiles, capped to a max
        float arcGap = 2f * Mathf.PI * wheelRadius / count;
        float tileSize = Mathf.Clamp(arcGap * 0.75f, 60f, 110f);

        for (int i = 0; i < count; i++)
        {
            GameObject tileGO = Instantiate(letterTilePrefab, wheelContainer);
            tileGO.name = $"Tile_{currentLetters[i]}_{i}";

            RectTransform rt = tileGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(tileSize, tileSize);

            float angle = startAngle - i * angleStep;
            float rad = angle * Mathf.Deg2Rad;
            rt.anchoredPosition = new Vector2(
                Mathf.Cos(rad) * wheelRadius,
                Mathf.Sin(rad) * wheelRadius
            );

            // Scale font to match tile size
            TMP_Text txt = tileGO.GetComponentInChildren<TMP_Text>();
            if (txt != null)
                txt.fontSize = tileSize * 0.5f;

            LetterTile tile = tileGO.GetComponent<LetterTile>();
            tile.SetLetter(currentLetters[i]);
            tile.WheelIndex = i;

            tiles.Add(tile);
        }
    }

    public void ShuffleTiles()
    {
        if (tiles.Count <= 1) return;

        // Fisher-Yates shuffle of letter assignments (positions stay fixed)
        char[] chars = currentLetters.ToCharArray();
        for (int i = chars.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        currentLetters = new string(chars);
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].SetLetter(chars[i]);
            StartCoroutine(TweenHelper.PunchScale(tiles[i].transform, Vector3.one * 0.15f, 0.25f));
        }
    }

    public void DeselectAll()
    {
        foreach (var tile in tiles)
            tile.SetSelected(false);
    }

    public void ClearWheel()
    {
        foreach (var tile in tiles)
        {
            if (tile != null)
                Destroy(tile.gameObject);
        }
        tiles.Clear();
    }

    public List<LetterTile> Tiles => tiles;
}
