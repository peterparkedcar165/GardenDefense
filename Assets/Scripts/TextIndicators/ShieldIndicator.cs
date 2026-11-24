using UnityEngine;
using TMPro;

public static class ShieldIndicator
{
    public static void Spawn(Vector3 position, float amount)
    {
        GameObject go = Object.Instantiate(HealIndicator.Prefab, position, Quaternion.identity);
        HealIndicator indicator = go.GetComponent<HealIndicator>();
        if (indicator != null) indicator.Initialize(amount);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp != null) tmp.color = new Color(0.75f, 0.75f, 0.75f);
    }
}
