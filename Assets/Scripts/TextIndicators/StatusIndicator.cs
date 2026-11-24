using UnityEngine;
using TMPro;

public class StatusIndicator : TextIndicator
{
    // same launch speed as DamageIndicator
    protected override float MinVerticalSpeed => 1f;
    protected override float MaxVerticalSpeed => 1.25f;

    // loaded once on first use instead of a resources lookup on every status text
    private static GameObject prefab;

    public static void Spawn(Vector3 position, string text, Color color)
    {
        if (!SettingsManager.ShowStatusEffectIndicators) return;

        if (prefab == null) prefab = Resources.Load<GameObject>("StatusIndicator");
        GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
        StatusIndicator indicator = go.GetComponent<StatusIndicator>();
        if (indicator != null) indicator.Initialize(text, color);
    }

    public void Initialize(string text, Color color)
    {
        shrink = false;
        tmpText.color     = color;
        tmpText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmpText.fontSize += 1f;
        tmpText.fontSize *= 0.5f;
        tmpText.text      = text;
    }
}
