using UnityEngine;

[CreateAssetMenu(fileName = "AeoniumData", menuName = "Scriptable Objects/PlantData/Aeonium")]
public class AeoniumData : PlantData
{
    public int baseSunGenerated;
    public float baseSunTimerReduction;
    public float baseHealAmount;
    public float baseSkillCooldownReduction;
    public float baseSkillRangeBonus;
    public float skillRangeBonusPerLevel;
    public float baseSkillSpeedBonus;
    public float skillSpeedBonusPerLevel;

    public float healMPMultiplier;
    public float sunTimerMPMultiplier;
    public float skillSpeedMPMultiplier;
}
