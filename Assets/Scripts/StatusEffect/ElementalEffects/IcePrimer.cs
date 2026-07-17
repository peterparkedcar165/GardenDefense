using UnityEngine;

public class IcePrimer : ElementalDebuff
{
    public IcePrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Ice</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<FirePrimer>())
        {
            insect.RemoveEffect<IcePrimer>();
            insect.RemoveEffect<FirePrimer>();
            insect.ApplyEffect(new FractureEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<GrassPrimer>())
        {
            insect.RemoveEffect<IcePrimer>();
            insect.RemoveEffect<GrassPrimer>();
            insect.ApplyEffect(new BrittleEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<PoisonPrimer>())
        {
            insect.RemoveEffect<IcePrimer>();
            insect.RemoveEffect<PoisonPrimer>();
            insect.ApplyEffect(new FrostbiteEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<WaterPrimer>())
        {
            if (insect.freezeInternalCooldown <= 0)
            {
                insect.RemoveEffect<IcePrimer>();
                insect.RemoveEffect<WaterPrimer>();
                insect.freezeInternalCooldown = 5f;
                insect.ApplyEffect(new FreezeEffect(insect, 3f * (1f + source.elementalAffinity), 1, source));
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
