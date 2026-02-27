using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "WordyWords/LevelData")]
public class LevelData : ScriptableObject
{
    public string letters;
    public int gridWidth;
    public int gridHeight;
    public List<WordPlacement> wordPlacements = new List<WordPlacement>();
    public List<string> extraWords = new List<string>();
    public Sprite backgroundSprite;
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.25f, 1f);

    public HashSet<string> GetGridWordSet()
    {
        var set = new HashSet<string>();
        foreach (var wp in wordPlacements)
            set.Add(wp.word.ToUpper());
        return set;
    }

    public HashSet<string> GetExtraWordSet()
    {
        var set = new HashSet<string>();
        foreach (var w in extraWords)
            set.Add(w.ToUpper());
        return set;
    }
}
