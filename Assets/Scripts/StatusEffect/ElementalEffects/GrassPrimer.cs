using UnityEngine;

public class GrassPrimer : ElementalDebuff
{
    public GrassPrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Grass;
    }

    public override string GetName() => "<color=green>Grass</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<FirePrimer>())
        {
            insect.RemoveEffect<GrassPrimer>();
            insect.RemoveEffect<FirePrimer>();
            insect.ApplyEffect(new BurnEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<IcePrimer>())
        {
            insect.RemoveEffect<GrassPrimer>();
            insect.RemoveEffect<IcePrimer>();
            insect.ApplyEffect(new BrittleEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<PoisonPrimer>())
        {
            insect.RemoveEffect<GrassPrimer>();
            insect.RemoveEffect<PoisonPrimer>();
            insect.ApplyEffect(new PoisonedEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<WaterPrimer>())
        {
            if (insect.germinateInternalCooldown <= 0)
            {
                insect.RemoveEffect<GrassPrimer>();
                insect.RemoveEffect<WaterPrimer>();
                insect.germinateInternalCooldown = 2f;
                insect.ApplyEffect(new GerminateEffect(insect, 2f, 1, source));
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
