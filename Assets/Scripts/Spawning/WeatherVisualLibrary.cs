using UnityEngine;

// the one place every weather type's look is authored: how far the scene light tints toward a
// color, how it scales the light's intensity, and which particle prefab represents it. one
// asset, referenced by every level's WeatherManager - add a new weather's look here and every
// level that ever sets that weather picks it up automatically
[CreateAssetMenu(fileName = "WeatherVisualLibrary", menuName = "Scriptable Objects/Weather Visual Library")]
public class WeatherVisualLibrary : ScriptableObject
{
    public WeatherVisualEntry[] entries;

    public WeatherVisualEntry Find(WeatherType type)
    {
        if (entries == null) return null;
        foreach (WeatherVisualEntry entry in entries)
            if (entry.weather == type) return entry;
        return null;
    }
}

[System.Serializable]
public class WeatherVisualEntry
{
    public WeatherType weather;
    [Tooltip("the scene light blends toward this color by tintStrength - 0 strength leaves the light's own color untouched")]
    public Color tintColor = Color.white;
    [Range(0f, 1f)] public float tintStrength = 0f;
    [Tooltip("multiplies the scene light's base intensity, e.g. 0.7 = 30% dimmer")]
    public float lightIntensityMultiplier = 1f;
    [Tooltip("enabled for as long as this weather is active on the level - leave empty for no particle effect")]
    public GameObject particlePrefab;
    [Tooltip("if set, swapped onto the particle instance's renderer when the scene's light is the Night one (e.g. Sandstorm looks different lit by NightLight vs DesertLight) - leave empty if this weather doesn't need a night variant")]
    public Material nightMaterial;
    [Tooltip("looping ambience that fades in while this weather is active and fades out when it ends - leave empty for no sound")]
    public AmbienceProfile ambience;
}
