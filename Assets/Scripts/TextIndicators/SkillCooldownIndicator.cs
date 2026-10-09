using UnityEngine;
using TMPro;

// Dandelion's passive pulse: shown on every allied plant whose Skill Cooldown just got reduced.
// reuses the HealIndicator prefab/sizing (same lightweight pattern ShieldIndicator uses) rather
// than needing its own prefab, then overrides the color and text
public static class SkillCooldownIndicator
{
    private static readonly Color WindColor = new Color(0.85f, 1f, 0.85f);

    public static void Spawn(Vector3 position, float amount)
    {
        GameObject go = Object.Instantiate(
            Resources.Load<GameObject>("HealIndicator"), position, Quaternion.identity);
        go.GetComponent<HealIndicator>()?.Initialize(amount);
        TMP_Text tmp = go.GetComponent<TMP_Text>();
        if (tmp == null) return;
        tmp.color = WindColor;
        tmp.fontStyle = FontStyles.Bold;
        tmp.text = $"-{Mathf.RoundToInt(amount)}";
    }
}
