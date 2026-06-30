using UnityEngine;

public class ChillEffect : StatusEffect
{
    private readonly float baseSlow;
    private readonly float slowPerLevel;

    private float TotalSlow => baseSlow + slowPerLevel * (level - 1);
    private const float IceResReduction = 0.24f;
    private bool ReducesIceResistance => source is Snowdrop sd && sd.IsPath2Maxed;

    public ChillEffect(Entity target, float duration, int level, Entity source,
                       float baseSlow = 0.24f, float slowPerLevel = 0.06f)
        : base(target, duration, level, source)
    {
        this.baseSlow     = baseSlow;
        this.slowPerLevel = slowPerLevel;
        effectType = Type.negative;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Chill</color>";

    public override string GetDescription()
    {
        string desc = $"Reduce <color=#00FFFF><b>Movement Speed</b></color> by <color=red><b>{TotalSlow * 100f:F0}%</b></color>";
        if (ReducesIceResistance)
            desc += $" and <color=#00FFFF><b>Ice Resistance</b></color> by <color=red><b>{IceResReduction * 100f:F0}%</b></color>";
        return desc + ".";
    }

    public override void OnApply()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier -= TotalSlow;
        if (ReducesIceResistance)
            target.iceResistanceAdder -= IceResReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.movementSpeedMultiplier += TotalSlow;
        if (ReducesIceResistance)
            target.iceResistanceAdder += IceResReduction;
    }
}
