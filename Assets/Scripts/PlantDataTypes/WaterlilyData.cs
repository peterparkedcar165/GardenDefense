using UnityEngine;

[CreateAssetMenu(fileName = "WaterlilyData", menuName = "Scriptable Objects/PlantData/Waterlily")]
public class WaterlilyData : PlantData
{
    public float baseAoERange;

    public override string GetAttackDescription() =>
        $"Blow little bubbles towards her target, dealing <color=green><b>{baseAttackDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic </color>damage.";

    public override string GetPassiveDescription() =>
        $"Attacks deal <color=green><b>{basePassiveDamage}</b></color> <color=#3399FF>Water</color> damage to surrounding insects within a <color=green><b>{baseAoERange}</b></color> radius.";

    public override string GetSkillDescription() =>
        $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color=green><b>{baseSkillDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic</color> damage upon impact, and keeping them airborne for <color=green><b>{baseSkillDuration}</b></color> seconds.";
}
