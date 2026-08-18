using UnityEngine;

[CreateAssetMenu(fileName = "BogIrisData", menuName = "Scriptable Objects/PlantData/BogIris")]
public class BogIrisData : PlantData
{
    public float baseOpenDuration;
    public float baseClosedHeal = 200f;
    public int baseSunGenerated;
    public float baseKnockUpHeight;
    public float baseGeyserDamage;
    public float openExtendChance = 0.35f;
    public float geyseredDuration = 8f;
    public float geyseredArmorShred = 20f;
    public float geyseredFallDamageResistanceShred = 0.15f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public float path2OpenDurationPerLevel = 2f;
    public int   path2SunPerLevel          = 1;
    public float path2ClosedHealPerLevel   = 20f;
    public float path2OpenExtendChancePerLevel = 0.03f;

    [Header("Path 3 Scaling")]
    public float path3GeyserDamagePerLevel = 15f;
    public float path3KnockUpPerLevel = 1f;
    public float path3GeyserRadiusPerLevel = 0.15f;

    public override string GetAttackDescription() =>
        $"Fires a water bolt at a single target dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        "Cycles between an <b><color=#4FC3F7>open</color></b> and <b><color=#4FC3F7>closed</color></b> state.\n\n" +
        "In <b><color=#4FC3F7>open</color></b> form, she generates <color=yellow>Sun</color> periodically, and attacks have a chance to extend the open state.\n\n" +
        "In <b><color=#4FC3F7>closed</color></b> form, she regenerates HP.";

    public override string GetSkillDescription() =>
        $"Target a location. After a brief delay, a geyser erupts, dealing {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage and knocking all insects airborne.";
}
