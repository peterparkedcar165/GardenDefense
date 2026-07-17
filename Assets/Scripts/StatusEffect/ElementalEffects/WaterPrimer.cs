using UnityEngine;

public class WaterPrimer : ElementalDebuff
{
    public WaterPrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Water;
    }

    public override string GetName() => "<color=#4FC3F7>Water</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<IcePrimer>())
        {
            if (insect.freezeInternalCooldown <= 0)
            {
                insect.RemoveEffect<WaterPrimer>();
                insect.RemoveEffect<IcePrimer>();
                insect.freezeInternalCooldown = 5f;
                insect.ApplyEffect(new FreezeEffect(insect, 3f * (1f + source.elementalAffinity), 1, source));
            }
        }
        else if (insect.HasEffect<PoisonPrimer>())
        {
            insect.RemoveEffect<WaterPrimer>();
            insect.RemoveEffect<PoisonPrimer>();
            insect.ApplyEffect(new SludgeEffect(insect, 4f, 1, source));
        }
        else if (insect.HasEffect<FirePrimer>())
        {
            insect.RemoveEffect<WaterPrimer>();
            insect.RemoveEffect<FirePrimer>();
            insect.ApplyEffect(new SteamEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<GrassPrimer>())
        {
            if (insect.germinateInternalCooldown <= 0)
            {
                insect.RemoveEffect<WaterPrimer>();
                insect.RemoveEffect<GrassPrimer>();
                insect.germinateInternalCooldown = 2f;
                insect.ApplyEffect(new GerminateEffect(insect, 2f, 1, source));
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
