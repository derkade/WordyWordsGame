using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CoinStreakManager : MonoBehaviour
{
    [Header("Pool")]
    [SerializeField] private int poolSize = 20;

    [Header("Appearance")]
    [SerializeField] private Color streakColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float streakWidth = 3f;
    [SerializeField] private float streakLength = 18f;

    [Header("Movement")]
    [SerializeField] private float travelDuration = 0.45f;
    [SerializeField] private float arcHeight = 0.25f;
    [SerializeField] private float staggerDelay = 0.08f;

    private RectTransform rectT;
    private GameObject[] poolObjects;
    private CoinStreakTrail[] poolTrails;
    private bool[] poolActive;
    private int[] availableStack;
    private int availableCount;

    private void Awake()
    {
        rectT = GetComponent<RectTransform>();

        // Generate a hard-edged streak texture: bright head fading to transparent tail
        int texW = 64, texH = 16;
        var tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float halfH = texH * 0.5f;
        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                // Horizontal: solid head, fades only in last 30% (tail)
                float xNorm = (float)x / (texW - 1);
                float headFade = xNorm > 0.3f ? 1f : xNorm / 0.3f;
                // Vertical: hard rectangular edge, 1px softening
                float dy = Mathf.Abs(y - halfH + 0.5f) / halfH;
                float edgeFade = dy < 0.7f ? 1f : Mathf.Clamp01((1f - dy) / 0.3f);
                float alpha = headFade * edgeFade;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        // Pivot at right-center (head of streak) so rotation works naturally
        var sprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(1f, 0.5f));

        // Initialize pool — same pattern as UIParticleEffect
        poolObjects = new GameObject[poolSize];
        poolTrails = new CoinStreakTrail[poolSize];
        poolActive = new bool[poolSize];
        availableStack = new int[poolSize];
        availableCount = poolSize;

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"CoinStreak_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(rectT, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(streakLength, streakWidth);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = streakColor;
            img.raycastTarget = false;

            var streak = go.AddComponent<CoinStreakTrail>();
            streak.Setup(rt, img, streakColor, streakLength);

            poolObjects[i] = go;
            poolTrails[i] = streak;
            poolActive[i] = false;
            availableStack[i] = i;

            go.SetActive(false);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < poolSize; i++)
        {
            if (!poolActive[i]) continue;

            bool finished = poolTrails[i].Tick(dt);
            if (finished)
                ReturnToPool(i);
        }
    }

    /// <summary>
    /// Convert world position to local anchoredPosition, same as UIParticleEffect.WorldToLocal
    /// </summary>
    private Vector2 WorldToLocal(Vector3 worldPos)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectT,
            RectTransformUtility.WorldToScreenPoint(cam, worldPos),
            cam,
            out Vector2 localPos);
        return localPos;
    }

    public void PlayStreaks(List<RectTransform> cellTransforms, Transform target)
    {
        if (cellTransforms == null || cellTransforms.Count == 0) return;

        Vector2 targetLocal = WorldToLocal(target.position);

        for (int i = 0; i < cellTransforms.Count; i++)
        {
            if (availableCount <= 0) break;

            int idx = availableStack[--availableCount];
            poolActive[idx] = true;

            Vector2 startLocal = WorldToLocal(cellTransforms[i].position);
            float delay = i * staggerDelay;

            poolObjects[idx].SetActive(true);
            poolObjects[idx].transform.SetAsLastSibling();
            poolTrails[idx].Initialize(startLocal, targetLocal, delay, travelDuration, arcHeight);
        }
    }

    private void ReturnToPool(int idx)
    {
        poolActive[idx] = false;
        poolObjects[idx].SetActive(false);
        availableStack[availableCount++] = idx;
    }
}
