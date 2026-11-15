using UnityEngine;

[CreateAssetMenu(fileName = "SunsetMulchData", menuName = "Scriptable Objects/Mulch/Sunset")]
public class SunsetMulchData : MulchData
{
    [Tooltip("extra sun ticks every sunflower fires when a wave ends")]
    public int extraTicks = 1;

    public override MulchEffect CreateEffect() => new SunsetMulchEffect(this);

    public override string GetDescription()
    {
        string ticks = extraTicks == 1 ? "one extra tick" : $"<b><color=green>{extraTicks}</color></b> extra ticks";
        return $"When a wave ends, every {TargetPlantLabel("Sunflower")} generates <b><color=#FFD700>Sun</color></b> ({ticks}).";
    }
}
