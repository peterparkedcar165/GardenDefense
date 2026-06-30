using UnityEngine;

public class TaintedEffect : ElementalDebuff
{
    public TaintedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Poison;
    }

    public override string GetName() => "<color=purple>Tainted</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<ColdEffect>())
        {
            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<ColdEffect>();
            insect.ApplyEffect(new FrostbiteEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<BlazeEffect>())
        {
            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<BlazeEffect>();
            insect.ApplyEffect(new VulnerableEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<SproutEffect>();
            insect.ApplyEffect(new PoisonedEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<WetEffect>())
        {
            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<WetEffect>();
            insect.ApplyEffect(new SludgeEffect(insect, 4f, 1, source));
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
