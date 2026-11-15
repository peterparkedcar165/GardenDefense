using UnityEngine;

// the bonus itself comes from the waterlily that soaked the insect (Waterlily.BubbleDamageTaken),
// so this mulch has no numbers of its own. the description reads them off the waterlily's data
[CreateAssetMenu(fileName = "WaterloggedMulchData", menuName = "Scriptable Objects/Mulch/Waterlogged")]
public class WaterloggedMulchData : MulchData
{
    public override MulchEffect CreateEffect() => new WaterloggedMulchEffect(this);

    public override string GetDescription()
    {
        WaterlilyData lily = targetPlant as WaterlilyData;
        float baseBonus = lily != null ? lily.baseBubbleDamageTaken : 0.08f;
        float perLevel = lily != null ? lily.path3DamageTakenPerLevel : 0.04f;
        float perMagicPower = lily != null ? lily.bubbleDamageTakenMP : 0.05f;
        string soaked = "<color=#4FC3F7><b>Soaked</b></color>";
        string bubble = "<color=#4FC3F7><b>Bubble Prison</b></color>";
        return $"Insects at max {soaked} stacks take the same increased damage as {bubble}: " +
            $"<b><color=green>{baseBonus * 100f:F0}%</color></b> + <b><color=green>{perLevel * 100f:F0}%</color></b> per Skill level " +
            $"[<color=#FFB6C1><b>+{perMagicPower * 100f:F0}% per 100 Magic Power</b></color>], from the {TargetPlantLabel("Waterlily")} that soaked them. " +
            $"It doesn't stack with {bubble}.";
    }
}
