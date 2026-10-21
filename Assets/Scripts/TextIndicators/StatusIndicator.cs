using UnityEngine;
using TMPro;

public class StatusIndicator : TextIndicator
{
    // same launch speed as DamageIndicator
    protected override float MinVerticalSpeed => 1f;
    protected override float MaxVerticalSpeed => 1.25f;

    public static void Spawn(Vector3 position, string text, Color color)
    {
        if (!SettingsManager.ShowStatusEffectIndicators) return;

        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("StatusIndicator"), position, Quaternion.identity);
        go.GetComponent<StatusIndicator>()?.Initialize(text, color);
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
