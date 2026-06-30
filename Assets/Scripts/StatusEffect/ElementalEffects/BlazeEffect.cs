using UnityEngine;

public class BlazeEffect : ElementalDebuff
{
    public BlazeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Fire;
    }

    public override string GetName() => "<color=orange>Blaze</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<ColdEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<ColdEffect>();
            insect.ApplyEffect(new FractureEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<SproutEffect>();
            insect.ApplyEffect(new BurnEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<TaintedEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<TaintedEffect>();
            insect.ApplyEffect(new VulnerableEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<WetEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<WetEffect>();
            insect.ApplyEffect(new BoilEffect(insect, 8f, 1, source));
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
