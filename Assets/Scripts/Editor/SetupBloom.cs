using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public static class SetupBloom
{
    [MenuItem("Tools/Setup Bloom")]
    public static void Setup()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/DefaultVolumeProfile.asset");
        if (profile == null)
        {
            Debug.LogError("DefaultVolumeProfile not found!");
            return;
        }

        Bloom bloom;
        if (!profile.TryGet(out bloom))
        {
            bloom = profile.Add<Bloom>();
        }

        bloom.active = true;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 1.0f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.5f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.7f;
        bloom.highQualityFiltering.overrideState = true;
        bloom.highQualityFiltering.value = true;

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        Debug.Log("Bloom configured: threshold=1.0, intensity=1.5, scatter=0.7, HQ=true");
    }
}
