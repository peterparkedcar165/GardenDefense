using UnityEngine;
using TMPro;

public class HealIndicator : TextIndicator
{
    public static void Spawn(Vector3 position, float amount, bool isCrit = false)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("HealIndicator"), position, Quaternion.identity);
        go.GetComponent<HealIndicator>()?.Initialize(amount, isCrit);
    }

    public void Initialize(float amount, bool isCrit = false)
    {
        tmpText.color     = isCrit ? new Color(0.4f, 1f, 0.4f) : new Color(0.2f, 1f, 0.2f);
        tmpText.fontStyle = FontStyles.Bold;
        if (isCrit) tmpText.fontSize *= 1.5f;
        int rounded = Mathf.RoundToInt(amount);
        tmpText.text = isCrit ? $"+{rounded}!" : $"+{rounded}";
    }
}
