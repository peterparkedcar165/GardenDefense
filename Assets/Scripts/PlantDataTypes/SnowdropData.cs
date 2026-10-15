using UnityEngine;

[CreateAssetMenu(fileName = "SnowdropData", menuName = "Scriptable Objects/PlantData/Snowdrop")]
public class SnowdropData : PlantData
{
    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 3f;
    public float path1AttackSpeedPerLevel  = 0.05f;
    public float path1AttackRangePerLevel  = 0.2f;

    [Header("Path 2 - Snow Mark")]
    public float snowMarkFlatDamageBase        = 12f;
    public float snowMarkFlatDamagePerLevel    = 6f;
    public float snowMarkDamagePercentBase     = 0.02f;
    public float snowMarkDamagePercentPerLevel = 0.01f;
    // Magic Power scaling: +1% per 100 Magic Power, same convention as every other MP-scaled stat
    public float snowMarkDamagePercentMP       = 0.01f;
    public float snowMarkDuration = 6f;

    [Header("Path 3 - Ice Beam")]
    public float iceBeamDurationPerLevel         = 1f;
    public float iceBeamAttackSpeed              = 3f;
    public float iceBeamAttackSpeedPerLevel      = 0.4f;
    // Path3 max: while the beam is active, the bonded partner also gains this much Attack Speed
    public float path3MaxBondedPartnerHasteBonus = 0.15f;

    public override string GetAttackDescription() =>
        $"Fires a magical snowball at her target, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        $"Bond with a plant. Attacks apply <color=#00FFFF><b>Snow Mark</b></color> and deal bonus {DamageTypeLabel(damageType)} equal to a % of the target's <color=red><b>Max Health</b></color>. The bonded plant can detonate the <color=#00FFFF><b>Mark</b></color> for the same bonus damage.";

    public override string GetSkillDescription() =>
        "<color=#00FFFF><b>Snowdrop</b></color>'s attacks become an <color=#00FFFF><b>Ice Beam</b></color> with a fixed Attack Speed.";
}
