using UnityEngine;

public class GroundPrimer : ElementalDebuff
{
    public GroundPrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Ground;
    }

    public override string GetName() => "<color=#79391F>Ground</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    // no elemental reactions yet
    public override void OnApply() { }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
