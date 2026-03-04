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
    [SerializeField] private float glowIntensity = 3f;

    [Header("Trail")]
    [SerializeField] private float trailSpan = 0.3f;
    [SerializeField] private int trailSamples = 10;

    [Header("Movement")]
    public float TravelDuration => travelDuration;
    public float StaggerDelay => staggerDelay;
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

        // Create glow material from UIGlow shader
        Material glowMat = null;
        Shader glowShader = Shader.Find("UI/Glow");
        if (glowShader != null)
        {
            glowMat = new Material(glowShader);
            glowMat.SetFloat("_GlowIntensity", glowIntensity);
        }

        poolObjects = new GameObject[poolSize];
        poolTrails = new CoinStreakTrail[poolSize];
        poolActive = new bool[poolSize];
        availableStack = new int[poolSize];
        availableCount = poolSize;

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"CoinStreak_{i}", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(rectT, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var streak = go.AddComponent<CoinStreakTrail>();
            streak.Setup(streakColor, streakWidth, trailSamples, trailSpan);
            if (glowMat != null)
                streak.material = glowMat;

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

    public void PlayStreaks(List<RectTransform> cellTransforms, Vector3 targetWorldPos)
    {
        if (cellTransforms == null || cellTransforms.Count == 0) return;

        Vector2 targetLocal = WorldToLocal(targetWorldPos);

        for (int i = 0; i < cellTransforms.Count; i++)
        {
            if (availableCount <= 0) break;

            int idx = availableStack[--availableCount];
            poolActive[idx] = true;

            Vector2 startLocal = WorldToLocal(cellTransforms[i].position);
            float delay = i * staggerDelay;

            // Random arc variant: upward, mirrored downward, or straight
            float arc;
            float roll = Random.value;
            if (roll < 0.33f)
                arc = -arcHeight;          // mirrored downward
            else if (roll < 0.66f)
                arc = arcHeight * 0.1f;    // mostly straight
            else
                arc = arcHeight;           // normal upward

            poolObjects[idx].SetActive(true);
            poolObjects[idx].transform.SetAsLastSibling();
            poolTrails[idx].Initialize(startLocal, targetLocal, delay, travelDuration, arc);
        }
    }

    private void ReturnToPool(int idx)
    {
        poolActive[idx] = false;
        poolObjects[idx].SetActive(false);
        availableStack[availableCount++] = idx;
    }
}
