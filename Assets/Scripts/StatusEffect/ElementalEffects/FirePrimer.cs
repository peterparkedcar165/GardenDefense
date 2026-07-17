using UnityEngine;

public class FirePrimer : ElementalDebuff
{
    public FirePrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Fire;
    }

    public override string GetName() => "<color=orange>Fire</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<IcePrimer>())
        {
            insect.RemoveEffect<FirePrimer>();
            insect.RemoveEffect<IcePrimer>();
            insect.ApplyEffect(new FractureEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<GrassPrimer>())
        {
            insect.RemoveEffect<FirePrimer>();
            insect.RemoveEffect<GrassPrimer>();
            insect.ApplyEffect(new BurnEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<PoisonPrimer>())
        {
            insect.RemoveEffect<FirePrimer>();
            insect.RemoveEffect<PoisonPrimer>();
            insect.ApplyEffect(new VulnerableEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<WaterPrimer>())
        {
            insect.RemoveEffect<FirePrimer>();
            insect.RemoveEffect<WaterPrimer>();
            insect.ApplyEffect(new SteamEffect(insect, 8f, 1, source));
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
