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

    [Header("Glow")]
    [Tooltip("Width multiplier for the additive glow layer (e.g. 3 = glow is 3x wider than core)")]
    [SerializeField] private float glowSize = 3f;
    [Tooltip("Glow shader intensity (HDR brightness for bloom)")]
    [SerializeField] private float glowIntensity = 2f;

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

    // Core layer: opaque, hard-edged, default UI material
    private GameObject[] poolCoreObjects;
    private CoinStreakTrail[] poolCoreTrails;

    // Glow layer: additive, wider, UI/Glow shader
    private GameObject[] poolGlowObjects;
    private CoinStreakTrail[] poolGlowTrails;

    private bool[] poolActive;
    private int[] availableStack;
    private int availableCount;

    private void Awake()
    {
        rectT = GetComponent<RectTransform>();

        // Create additive glow material
        Material glowMat = null;
        Shader glowShader = Shader.Find("UI/Glow");
        if (glowShader != null)
        {
            glowMat = new Material(glowShader);
            glowMat.SetFloat("_GlowIntensity", glowIntensity);
        }

        poolCoreObjects = new GameObject[poolSize];
        poolCoreTrails = new CoinStreakTrail[poolSize];
        poolGlowObjects = new GameObject[poolSize];
        poolGlowTrails = new CoinStreakTrail[poolSize];
        poolActive = new bool[poolSize];
        availableStack = new int[poolSize];
        availableCount = poolSize;

        for (int i = 0; i < poolSize; i++)
        {
            // Glow layer (renders first, behind core)
            var glowGO = CreateTrailGO($"CoinStreak_Glow_{i}", streakColor, streakWidth * glowSize, glowMat);
            poolGlowObjects[i] = glowGO;
            poolGlowTrails[i] = glowGO.GetComponent<CoinStreakTrail>();

            // Core layer (renders on top, opaque)
            var coreGO = CreateTrailGO($"CoinStreak_Core_{i}", streakColor, streakWidth, null);
            poolCoreObjects[i] = coreGO;
            poolCoreTrails[i] = coreGO.GetComponent<CoinStreakTrail>();

            poolActive[i] = false;
            availableStack[i] = i;
        }
    }

    private GameObject CreateTrailGO(string name, Color color, float width, Material mat)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(rectT, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var streak = go.AddComponent<CoinStreakTrail>();
        streak.Setup(color, width, trailSamples, trailSpan);
        if (mat != null)
            streak.material = mat;

        go.SetActive(false);
        return go;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < poolSize; i++)
        {
            if (!poolActive[i]) continue;

            // Core drives the animation, glow syncs from it
            bool finished = poolCoreTrails[i].Tick(dt);
            poolGlowTrails[i].SyncFrom(poolCoreTrails[i]);

            if (finished)
                ReturnToPool(i);
        }
    }

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

            // Initialize core (drives animation)
            poolCoreObjects[idx].SetActive(true);
            poolCoreTrails[idx].Initialize(startLocal, targetLocal, delay, travelDuration, arc);

            // Glow copies the exact same Bezier path
            poolGlowObjects[idx].SetActive(true);
            poolGlowTrails[idx].CopyPathFrom(poolCoreTrails[idx]);

            // Render order: core behind, glow on top
            poolCoreObjects[idx].transform.SetAsLastSibling();
            poolGlowObjects[idx].transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Play a single streak from one world position to another.
    /// Optional color override (e.g., blue for bonus words, default gold for hints).
    /// </summary>
    public void PlaySingleStreak(Vector3 fromWorldPos, Vector3 toWorldPos, Color? colorOverride = null)
    {
        if (availableCount <= 0) return;

        int idx = availableStack[--availableCount];
        poolActive[idx] = true;

        if (colorOverride.HasValue)
        {
            poolCoreTrails[idx].SetColor(colorOverride.Value);
            poolGlowTrails[idx].SetColor(colorOverride.Value);
        }

        Vector2 startLocal = WorldToLocal(fromWorldPos);
        Vector2 endLocal = WorldToLocal(toWorldPos);

        float arc;
        float roll = Random.value;
        if (roll < 0.33f)
            arc = -arcHeight;
        else if (roll < 0.66f)
            arc = arcHeight * 0.1f;
        else
            arc = arcHeight;

        poolCoreObjects[idx].SetActive(true);
        poolCoreTrails[idx].Initialize(startLocal, endLocal, 0f, travelDuration, arc);

        poolGlowObjects[idx].SetActive(true);
        poolGlowTrails[idx].CopyPathFrom(poolCoreTrails[idx]);

        poolCoreObjects[idx].transform.SetAsLastSibling();
        poolGlowObjects[idx].transform.SetAsLastSibling();
    }

    private void ReturnToPool(int idx)
    {
        poolActive[idx] = false;
        poolCoreObjects[idx].SetActive(false);
        poolGlowObjects[idx].SetActive(false);
        // Reset color back to default
        poolCoreTrails[idx].SetColor(streakColor);
        poolGlowTrails[idx].SetColor(streakColor);
        availableStack[availableCount++] = idx;
    }
}
