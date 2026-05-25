using UnityEngine;

public class ColdEffect : ElementalDebuff
{
    public ColdEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {

    }

    public override string GetName() => "<color=#00BFFF>Cold</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<BlazeEffect>())
        {
            insect.RemoveEffect<ColdEffect>();
            insect.RemoveEffect<BlazeEffect>();
            insect.ApplyEffect(new FractureEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<ColdEffect>();
            insect.RemoveEffect<SproutEffect>();
            insect.ApplyEffect(new BrittleEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<TaintedEffect>())
        {
            insect.RemoveEffect<ColdEffect>();
            insect.RemoveEffect<TaintedEffect>();
            insect.ApplyEffect(new FrostbiteEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<WetEffect>())
        {
            if (insect.freezeInternalCooldown <= 0)
            {
                insect.RemoveEffect<ColdEffect>();
                insect.RemoveEffect<WetEffect>();
                insect.freezeInternalCooldown = 5f;
                insect.ApplyEffect(new FreezeEffect(insect, 4f + source.elementalPower * 1.25f, 1, source));
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
