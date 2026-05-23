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

    public override string GetAttackDescription() =>
        $"Every <color=green><b>{(baseAttackSpeed > 0f ? 1f / baseAttackSpeed : 0f):F1}s</b></color>, pulses restorative energy to all plants within range, " +
        $"healing them for <color=green><b>{baseHealAmount:F0}</b></color> health (<color=green><b>{baseHealAmount * 0.5f:F0}</b></color> to herself) " +
        $"and reducing their Skill Cooldown by <color=green><b>{baseSkillCooldownReduction:F1}s</b></color>.";

    public override string GetPassiveDescription() =>
        $"Generates <color=yellow><b>{baseSunGenerated}</b></color> <color=yellow>Sun</color> every <color=green><b>{basePassiveCooldown:F0}s</b></color>. " +
        $"Each Attack-tagged hit by a plant within range reduces the timer by <color=green><b>{baseSunTimerReduction:F1}s</b></color>.";

    public override string GetSkillDescription() =>
        $"Increases her own Attack Range by <color=green><b>{baseSkillRangeBonus * 100f:F0}%</b></color> and Attack Speed by <color=green><b>{baseSkillSpeedBonus * 100f:F0}%</b></color> " +
        $"for <color=green><b>{baseSkillDuration:F0}s</b></color>. Insects that die within her range during this time yield bonus <color=yellow><b>Sun</b></color>.";
}
