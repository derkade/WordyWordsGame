using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIParticleEffect : MonoBehaviour
{
    [Header("Particle Settings")]
    [Tooltip("Number of particles emitted per burst")]
    [SerializeField] private int particleCount = 15;
    [Tooltip("Initial speed of particles in pixels per second")]
    [SerializeField] private float speed = 300f;
    [Tooltip("Maximum lifetime of particles in seconds")]
    [SerializeField] private float lifetime = 0.8f;
    [Tooltip("Initial size of each particle in pixels")]
    [SerializeField] private float startSize = 20f;
    [Tooltip("Tint color applied to particles (clamped to 0-1 by vertex colors)")]
    [SerializeField] private Color particleColor = new Color(1f, 0.84f, 0f, 1f);
    [Tooltip("Whether particles are affected by downward gravity")]
    [SerializeField] private bool useGravity = true;
    [Tooltip("Gravity strength in pixels per second squared")]
    [SerializeField] private float gravity = 400f;
    [Tooltip("Sprite used for particles. If empty, a soft circle is generated at runtime")]
    [SerializeField] private Sprite particleSprite;
    [Tooltip("Material with UI/Glow shader for additive glow blending")]
    [SerializeField] private Material glowMaterial;
    [Header("Glow Texture")]
    [Tooltip("Power curve for glow texture falloff (higher = tighter bright core)")]
    [SerializeField] private float glowFalloff = 2f;
    [Tooltip("Brightness multiplier baked into the glow texture core")]
    [SerializeField] private float glowBrightness = 1.5f;

    private RectTransform rectT;
    private List<RectTransform> pool = new List<RectTransform>();

    private void Awake()
    {
        rectT = GetComponent<RectTransform>();
        if (particleSprite == null)
            particleSprite = GenerateSoftCircle();
    }

    private Sprite GenerateSoftCircle()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / center;
                float distClamped = Mathf.Clamp01(dist);

                // Power curve falloff: tight bright core with soft edges
                float alpha = Mathf.Pow(1f - distClamped, glowFalloff);

                // Brightness boost in the core (baked fake HDR)
                float brightness = Mathf.Min(alpha * glowBrightness, 1f);

                tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Stop all active particle coroutines and hide active particles.
    /// </summary>
    public void Stop()
    {
        StopAllCoroutines();
        foreach (var rt in pool)
        {
            if (rt != null)
                rt.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Fire particles from the center of this object.
    /// </summary>
    public void Play()
    {
        StartCoroutine(EmitBurst(Vector2.zero));
    }

    /// <summary>
    /// Fire particles at a target RectTransform's position (converted to local space).
    /// </summary>
    public void PlayAt(RectTransform target)
    {
        Vector2 localPos = WorldToLocal(target);
        StartCoroutine(EmitBurst(localPos));
    }

    /// <summary>
    /// Fire staggered bursts at each target in order, with a delay between each.
    /// </summary>
    public void PlaySequence(List<RectTransform> targets, float staggerDelay = 0.1f)
    {
        StartCoroutine(EmitSequence(targets, staggerDelay));
    }

    /// <summary>
    /// Fire multiple bursts at random positions like fireworks over a duration.
    /// </summary>
    public void PlayFireworks(int burstCount = 8, float duration = 2.5f)
    {
        StartCoroutine(EmitFireworks(burstCount, duration));
    }

    private static readonly Color[] fireworkColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f, 1f),     // red
        new Color(1f, 0.84f, 0f, 1f),      // gold
        new Color(0.3f, 1f, 0.5f, 1f),     // green
        new Color(0.4f, 0.6f, 1f, 1f),     // blue
        new Color(1f, 0.5f, 0f, 1f),       // orange
        new Color(1f, 0.4f, 0.8f, 1f),     // pink
        new Color(0.7f, 0.4f, 1f, 1f),     // purple
        new Color(1f, 1f, 0.3f, 1f),       // yellow
    };

    private IEnumerator EmitFireworks(int burstCount, float duration)
    {
        Rect rect = rectT.rect;
        float delay = duration / burstCount;

        for (int i = 0; i < burstCount; i++)
        {
            float x = Random.Range(rect.xMin * 0.8f, rect.xMax * 0.8f);
            float y = Random.Range(rect.yMin * 0.5f, rect.yMax * 0.8f);
            Color color = fireworkColors[Random.Range(0, fireworkColors.Length)];

            StartCoroutine(EmitBurst(new Vector2(x, y), color));
            yield return new WaitForSeconds(delay * Random.Range(0.5f, 1.2f));
        }
    }

    private IEnumerator EmitSequence(List<RectTransform> targets, float staggerDelay)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Vector2 localPos = WorldToLocal(targets[i]);
            StartCoroutine(EmitBurst(localPos));
            if (i < targets.Count - 1)
                yield return new WaitForSeconds(staggerDelay);
        }
    }

    private Vector2 WorldToLocal(RectTransform target)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector3 worldPos = target.position;
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectT,
            RectTransformUtility.WorldToScreenPoint(cam, worldPos),
            cam,
            out localPos);
        return localPos;
    }

    private IEnumerator EmitBurst(Vector2 origin, Color? colorOverride = null)
    {
        var particles = new List<ParticleData>();
        Color burstColor = colorOverride ?? particleColor;

        for (int i = 0; i < particleCount; i++)
        {
            RectTransform p = GetOrCreateParticle();
            p.gameObject.SetActive(true);
            p.anchoredPosition = origin;
            p.sizeDelta = new Vector2(startSize, startSize);
            p.localScale = Vector3.one;

            Image img = p.GetComponent<Image>();
            img.color = burstColor;

            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float spd = speed * Random.Range(0.5f, 1.2f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spd;

            particles.Add(new ParticleData
            {
                rt = p,
                img = img,
                velocity = velocity,
                age = 0f,
                maxAge = lifetime * Random.Range(0.7f, 1.0f),
                startColor = burstColor
            });
        }

        float elapsed = 0f;
        float maxLife = lifetime;

        while (elapsed < maxLife)
        {
            float dt = Time.deltaTime;
            elapsed += dt;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var pd = particles[i];
                pd.age += dt;

                if (pd.age >= pd.maxAge)
                {
                    pd.rt.gameObject.SetActive(false);
                    particles.RemoveAt(i);
                    continue;
                }

                if (useGravity)
                    pd.velocity.y -= gravity * dt;

                pd.rt.anchoredPosition += pd.velocity * dt;

                float t = pd.age / pd.maxAge;
                float fade = 1f - t;
                float scale = 1f - t * 0.5f;
                pd.img.color = new Color(pd.startColor.r * fade, pd.startColor.g * fade, pd.startColor.b * fade, fade);
                pd.rt.localScale = Vector3.one * scale;

                particles[i] = pd;
            }

            yield return null;
        }

        foreach (var pd in particles)
            pd.rt.gameObject.SetActive(false);
    }

    private RectTransform GetOrCreateParticle()
    {
        foreach (var p in pool)
        {
            if (!p.gameObject.activeSelf)
                return p;
        }

        GameObject go = new GameObject("Particle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(rectT, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        if (particleSprite != null)
            img.sprite = particleSprite;
        if (glowMaterial != null)
            img.material = glowMaterial;

        pool.Add(rt);
        return rt;
    }

    private struct ParticleData
    {
        public RectTransform rt;
        public Image img;
        public Vector2 velocity;
        public float age;
        public float maxAge;
        public Color startColor;
    }
}
