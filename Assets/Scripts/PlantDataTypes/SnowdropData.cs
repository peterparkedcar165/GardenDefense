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
    public float snowMarkDuration = 6f;

    [Header("Path 3 - Ice Beam")]
    public float iceBeamDurationPerLevel = 1f;
    public float iceBeamAttackSpeed      = 4f;

    public override string GetAttackDescription() =>
        $"Fires an ice projectile at its target, dealing {DamageTypeLabel(damageType)}.";

    public override string GetPassiveDescription() =>
        "Attacks apply <color=#00FFFF>Snow Mark</color> and deal bonus <color=#00FFFF>Ice</color> Magic damage equal to a % of the target's Max Health. Can bond with any plant on the field, letting it detonate the Mark.";

    public override string GetSkillDescription() =>
        "Converts attacks into a piercing <color=#00FFFF>Ice Beam</color> with a fixed Attack Speed.";
}
