using UnityEngine;

[CreateAssetMenu(fileName = "HelleboreData", menuName = "Scriptable Objects/PlantData/Hellebore")]
public class HelleboreData : PlantData
{
    [Header("Path 1 - Attack")]
    public float path1AttackSpeedPerLevel = 0.05f;
    public float path1MagicPowerPerLevel  = 5f;

    [Header("Passive - Self Physical Resistance")]
    public float selfPhysResistBase      = 0.15f;
    public float selfPhysResistPerLevel  = 0.02f;
    public float selfPhysResistMP        = 0.002f;
    public float passiveCDRPerHit        = 0.5f;

    [Header("Path 2 - Passive")]
    public float path2CDRPerLevel        = 0.1f;
    public float path2AuraSharePerLevel  = 0.05f;

    [Header("Passive - Aura")]
    public float auraShareBase           = 0.5f;

    [Header("Skill - Thorned Guard")]
    public float shieldAmount      = 120f;
    public float shieldMP          = 0.5f;
    public float shieldDuration    = 12f;
    public float reflectPoisonBase = 15f;
    public float reflectPoisonMP   = 0.2f;

    [Header("Path 3 - Skill")]
    public float path3ShieldPerLevel   = 30f;
    public float path3DurationPerLevel = 2f;
    public float path3ReflectPerLevel  = 5f;

    public override string GetAttackDescription() =>
        $"Fires a thorned projectile dealing <color=green><b>{baseAttackDamage:F0}</b></color> " +
        $"{ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage.";

    public override string GetPassiveDescription() =>
        $"Each attack hit reduces skill cooldown by <color=green><b>{passiveCDRPerHit:F1}s</b></color>. " +
        $"Hellebore gains <color=#A0522D>Physical Resistance</color>: <color=green><b>{selfPhysResistBase * 100f:F0}%</b></color> base " +
        $"[<color=#FFB6C1><b>+{selfPhysResistMP * 10000f:F0}% per 100 MP</b></color>] per level. " +
        $"Plants within attack range gain <color=#9B30D0><b>Hellebore's Protection</b></color>: " +
        $"<color=green><b>{auraShareBase * 100f:F0}%</b></color> of Hellebore's Physical Resistance.";

    public override string GetSkillDescription() =>
        $"Targets a plant anywhere on the field, granting <color=#9B30D0><b>Thorned Guard</b></color>: " +
        $"a shield of <color=green><b>{shieldAmount:F0}</b></color> [<color=#FFB6C1><b>+{shieldMP * 100f:F0}% MP</b></color>] health for <color=green><b>{shieldDuration:F0}s</b></color>. " +
        $"While shielded, attackers receive <color=purple><b>{reflectPoisonBase:F0}</b></color> [<color=#FFB6C1><b>+{reflectPoisonMP * 100f:F0}% MP</b></color>] " +
        $"<color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per hit. " +
        $"Negative effects are reflected back to the attacker. The protection fades when the shield breaks.";
}
