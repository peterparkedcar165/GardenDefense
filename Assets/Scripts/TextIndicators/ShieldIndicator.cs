using UnityEngine;
using TMPro;

public static class ShieldIndicator
{
    public static void Spawn(Vector3 position, float amount)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("HealIndicator"), position, Quaternion.identity);
        go.GetComponent<HealIndicator>()?.Initialize(amount);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp != null) tmp.color = new Color(0.75f, 0.75f, 0.75f);
    }
}
