using UnityEngine;

[CreateAssetMenu(fileName = "CalendulaData", menuName = "Scriptable Objects/PlantData/Calendula")]
public class CalendulaData : PlantData
{
    public float baseFloralGlowHeal;
    public float baseSkillHealingMultiplier;

    public override string GetAttackDescription() =>
        $"Releases flaming petals dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage to all insects within range.";

    public override string GetPassiveDescription() =>
        "Illuminate the surrounding area with a radius equal to <color=green><b>1.5×</b></color> her Attack Range.";

    public override string GetSkillDescription() =>
        $"Target a plant anywhere on the field to grant <color=orange>Floral Glow</color> for <color=green><b>{baseSkillDuration:F0}s</b></color>. The plant's projectiles deal an additional <color=green><b>{baseAttackDamage:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage on hit. Heals the plant for <color=green><b>{baseFloralGlowHeal:F0}</b></color> health per second. Emits light equal to <b><color=orange>Calendula</color></b>'s range.";
}
