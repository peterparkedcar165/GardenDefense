using UnityEngine;
using TMPro;

public class HealIndicator : TextIndicator
{
    public static void Spawn(Vector3 position, float amount)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("HealIndicator"), position, Quaternion.identity);
        go.GetComponent<HealIndicator>()?.Initialize(amount);
    }

    public void Initialize(float amount)
    {
        tmpText.color     = new Color(0.2f, 1f, 0.2f);
        tmpText.fontStyle = FontStyles.Bold;
        tmpText.text      = $"+{amount:F0}";
    }
}
