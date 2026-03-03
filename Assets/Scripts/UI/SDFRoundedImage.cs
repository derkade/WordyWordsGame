using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the UI/RoundedRect SDF shader to this Image at runtime,
/// automatically sizing _RectSize from the actual RectTransform dimensions.
/// </summary>
[RequireComponent(typeof(Image))]
public class SDFRoundedImage : MonoBehaviour
{
    [SerializeField] private float cornerRadius = 24f;
    [SerializeField] private float borderWidth = 0f;
    [SerializeField] private Color borderColor = Color.black;

    [Header("Drop Shadow")]
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0);
    [SerializeField] private Vector2 shadowOffset = new Vector2(0f, -3f);
    [SerializeField] private float shadowBlur = 0f;
    [SerializeField] private float shadowExpand = 0f;

    [Header("Inner Bevel")]
    [SerializeField] private float bevelSize = 0f;
    [SerializeField] private float bevelStrength = 0f;

    private Material sdfMaterial;

    private void Awake()
    {
        var img = GetComponent<Image>();
        var shader = Shader.Find("UI/RoundedRect");
        if (shader == null || img == null) return;

        img.sprite = null;
        img.type = Image.Type.Simple;
        sdfMaterial = new Material(shader);
        sdfMaterial.SetFloat("_Radius", cornerRadius);
        sdfMaterial.SetFloat("_BorderWidth", borderWidth);
        sdfMaterial.SetColor("_BorderColor", borderColor);
        sdfMaterial.SetColor("_ShadowColor", shadowColor);
        sdfMaterial.SetVector("_ShadowOffset", new Vector4(shadowOffset.x, shadowOffset.y, 0, 0));
        sdfMaterial.SetFloat("_ShadowBlur", shadowBlur);
        sdfMaterial.SetFloat("_ShadowExpand", shadowExpand);
        sdfMaterial.SetFloat("_BevelSize", bevelSize);
        sdfMaterial.SetFloat("_BevelStrength", bevelStrength);
        img.material = sdfMaterial;
    }

    private void Start()
    {
        UpdateSize();
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateSize();
    }

    private void UpdateSize()
    {
        if (sdfMaterial == null) return;
        Rect r = GetComponent<RectTransform>().rect;
        if (r.width > 0 && r.height > 0)
            sdfMaterial.SetVector("_RectSize", new Vector4(r.width, r.height, 0, 0));
    }
}
