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
