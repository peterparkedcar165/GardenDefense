using UnityEngine;

[CreateAssetMenu(fileName = "HollyData", menuName = "Scriptable Objects/PlantData/Holly")]
public class HollyData : PlantData
{
    public float baseRetaliationHollyPercent;
    public float baseRetaliationInsectPercent;
    public float baseHealthBonusMP;
    public float baseFrozenRageReduction;
    public float baseFrozenRageReductionMP;
    public float baseSkillShield;
    public float baseSkillShieldMP;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 4f;
    public int path1ArmorPerLevel = 8;

    [Header("Path 2 Scaling")]
    public float path2HealthPerLevel = 40f;
    public float path2RetaliationPerLevel = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3ShieldPerLevel = 20f;
    public float path3FrozenRagePerLevel = 0.04f;
    public float path3SkillDurationPerLevel = 2f;

    public override string GetAttackDescription() =>
        $"Releases icy thorns dealing <color=green><b>{baseAttackDamage:F0}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage to all insects within range.";

    public override string GetPassiveDescription() =>
        $"Insects that attack <b><color=#00FFFF>Holly</color></b> retaliate for <color=green><b>{baseRetaliationHollyPercent * 100f:F0}%</b></color> of <b><color=#00FFFF>Holly</color></b>'s Attack Damage + " +
        $"<color=green><b>{baseRetaliationInsectPercent * 100f:F0}%</b></color> of the attacker's Attack Damage. " +
        $"Increases Max Health by <color=#FFB6C1><b>{baseHealthBonusMP * 100f:F0}%</b></color> Magic Power. " +
        $"While <b><color=#00FFFF>Holly</color></b> is shielded, insects within range are afflicted with <color=#00FFFF><b>Frozen Rage</b></color>, " +
        $"forcing them to target her and reducing their Armor by " +
        $"<color=green><b>{baseFrozenRageReduction * 100f / (1f - baseFrozenRageReduction):F0}</b></color> " +
        $"[+<color=#FFB6C1><b>{baseFrozenRageReductionMP * 100f:F0}%</b></color> Magic Power]. " +
        $"Taunted insects deal <color=#FF6666><b>50%</b></color> less Attack damage.";

    public override string GetSkillDescription() =>
        $"Grants <b><color=#00FFFF>Holly</color></b> a shield of <color=grey><b>{baseSkillShield:F0}</b></color> + <color=#FFB6C1><b>{baseSkillShieldMP * 100f:F0}% Magic Power</b></color> for <color=green><b>{baseSkillDuration:F0}s</b></color>. " +
        $"While the shield holds, nearby insects are forced to target <b><color=#00FFFF>Holly</color></b> via <color=#00FFFF><b>Frozen Rage</b></color>.";
}
