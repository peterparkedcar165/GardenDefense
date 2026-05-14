using UnityEngine;

public class DisplacedEffect : HardCrowdControl
{
    public DisplacedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#E0E0E0>Displaced</color>";
    public override string GetDescription() => "Knocked away — recovering balance.";

    public override void OnApply()
    {
        if (target is Insect insect) insect.lastSource = source;
    }
    public override void OnExpire() { }
}
