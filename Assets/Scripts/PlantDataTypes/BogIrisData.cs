using UnityEngine;

[CreateAssetMenu(fileName = "BogIrisData", menuName = "Scriptable Objects/PlantData/BogIris")]
public class BogIrisData : PlantData
{
    public float baseSunInterval = 4f;
    public int baseSunGenerated = 2;
    public float baseKnockUpHeight;
    public float baseGeyserDamage;
    public float geyseredDuration = 8f;
    public float geyseredArmorShred = 20f;
    public float geyseredFallDamageResistanceShred = 0.15f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 8f;
    public float path1AttackSpeedPerLevel = 0.05f;

    [Header("Path 2 Scaling")]
    public float baseRegenPercent = 0.02f;
    public float path2RegenPercentPerLevel = 0.01f;
    public int   baseOpenBonusSun = 2;
    public int   path2OpenBonusSunPerLevel = 1;
    public float baseReduceChance = 0.35f;
    public float path2ReduceChancePerLevel = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3GeyserDamagePerLevel = 15f;
    public float path3KnockUpPerLevel = 1f;
    public float path3GeyserRadiusPerLevel = 0.15f;

    public override string GetAttackDescription() =>
        $"Fires a water bolt at a single target dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Periodically generates <color=yellow>Sun</color>. When damaged, she <b><color=#4FC3F7>closes</color></b> and regenerates Max Health per second, doubled while out of combat. " +
        "When healthy, she <b><color=#4FC3F7>opens</color></b> and generates bonus <color=yellow>Sun</color> per production. Attacks have a chance to speed up the next Sun tick.";

    public override string GetSkillDescription() =>
        $"Target a location. After a brief delay, a geyser erupts, dealing {DamageTypeLabel(damageType)} and knocking all insects airborne.";
}
