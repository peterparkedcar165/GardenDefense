using UnityEngine;

public class PoisonPrimer : ElementalDebuff
{
    public PoisonPrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Poison;
    }

    public override string GetName() => "<color=purple>Poison</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<IcePrimer>())
        {
            insect.RemoveEffect<PoisonPrimer>();
            insect.RemoveEffect<IcePrimer>();
            insect.ApplyEffect(new FrostbiteEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<FirePrimer>())
        {
            insect.RemoveEffect<PoisonPrimer>();
            insect.RemoveEffect<FirePrimer>();
            insect.ApplyEffect(new VulnerableEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<GrassPrimer>())
        {
            insect.RemoveEffect<PoisonPrimer>();
            insect.RemoveEffect<GrassPrimer>();
            insect.ApplyEffect(new PoisonedEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<WaterPrimer>())
        {
            insect.RemoveEffect<PoisonPrimer>();
            insect.RemoveEffect<WaterPrimer>();
            insect.ApplyEffect(new SludgeEffect(insect, 4f, 1, source));
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
