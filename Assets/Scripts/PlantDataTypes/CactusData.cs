using UnityEngine;

[CreateAssetMenu(fileName = "CactusData", menuName = "Scriptable Objects/PlantData/Cactus")]
public class CactusData : PlantData
{
    public float baseShieldAmount = 100f;
    // Magic Power scaling: flat bonus per point of Cactus's own Magic Power, same convention as
    // AloeVera's heal scaling (direct coefficient, not a per-100 normalization)
    public float shieldMagicPowerScaling = 0.5f;

    [Header("Path 1 Scaling")]
    public float baseArmorScalingPercent = 0.5f;
    public float path1ArmorScalingPerLevel = 0.05f;
    public float path1ArmorPerLevel = 5f;

    [Header("Path 2 Scaling")]
    public float path2HealthPerLevel = 60f;
    public float baseCooldownRefundOnHit = 0.3f;
    public float path2CooldownRefundPerLevel = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3ShieldPerLevel = 10f;
    public float path3SkillDurationPerLevel = 2f;
    public float baseCounterReflectPercent = 0.5f;
    public float path3CounterReflectPerLevel = 0.1f;
    // Armor scaling: flat bonus added to the skill's reflected damage, same % pattern his Attack
    // already uses off his own Armor (see ArmorScalingPercent)
    public float counterArmorScalingPercent = 0.5f;

    public override string GetAttackDescription() =>
        $"Slams all insects in range, dealing {DamageTypeLabel(damageType)} plus a % of its Armor. Applies <color=#A0522D>Punctured</color>.";

    public override string GetPassiveDescription() =>
        "<color=#A0522D>Punctured</color> reduces Attack Damage instead of dealing bonus damage. Being shielded taunts nearby insects.";

    public override string GetSkillDescription() =>
        "Enters a countering stance and shields itself for a duration, gaining Physical Resistance and reflecting melee damage back at attackers.";
}
