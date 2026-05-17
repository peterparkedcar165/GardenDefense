using UnityEngine;

[CreateAssetMenu(fileName = "WaterlilyData", menuName = "Scriptable Objects/PlantData/Waterlily")]
public class WaterlilyData : PlantData
{
    public override string GetAttackDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Blow little bubbles towards her target, dealing <color=green><b>{s.attackDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic </color>damage.";
    }

    public override string GetPassiveDescription()
    {
        var s = plantPrefab.GetBaseStats();
        float splashDamage = s.attackDamage * 0.25f;
        return $"Attacks deal <color=green><b>{splashDamage}</b></color> <color=#3399FF>Water</color> damage to surrounding insects within a <color=green><b>{s.splashRadius}</b></color> radius.";
    }

    public override string GetSkillDescription()
    {
        var s = plantPrefab.GetBaseStats();
        return $"Blow a large bubble onto a targetted area, trapping insects within the bubble while dealing <color=green><b>{s.bubbleDamage}</b></color> <color=#3399FF>Water</color> <color=#FFB6C1>Magic</color> damage upon impact, and keeping them airborne for <color=green><b>{s.skillDuration}</b></color> seconds.";
    }
}
