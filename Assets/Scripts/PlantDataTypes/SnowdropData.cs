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
    public float path3MaxBondedPartnerHasteBonus = 0.35f;
}
