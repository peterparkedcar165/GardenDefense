using UnityEngine;
using TMPro;

public class HealIndicator : TextIndicator
{
    // mimics DamageIndicator's launch feel (faster initial rise, longer lifetime) but with no
    // gravity - it just floats straight up and fades, same arc shape as before gravity was added
    protected override float MinVerticalSpeed => 0.5f;
    protected override float MaxVerticalSpeed => 0.75f;

    public static void Spawn(Vector3 position, float amount, bool isCrit = false)
    {
        if (!SettingsManager.ShowHealingIndicators) return;

        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("HealIndicator"), position, Quaternion.identity);
        go.GetComponent<HealIndicator>()?.Initialize(amount, isCrit);
    }

    public void Initialize(float amount, bool isCrit = false)
    {
        lifetime = 2f;
        tmpText.color     = isCrit ? new Color(0.4f, 1f, 0.4f) : new Color(0.2f, 1f, 0.2f);
        tmpText.fontStyle = FontStyles.Bold;
        if (isCrit) tmpText.fontSize *= 1.5f;
        int rounded = Mathf.RoundToInt(amount);
        tmpText.text = isCrit ? $"+{rounded}!" : $"+{rounded}";
    }
}
