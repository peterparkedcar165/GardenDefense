using UnityEngine;

public class WetEffect : ElementalDebuff
{
    public WetEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {

    }

    public override string GetName() => "<color=#1E90FF>Wet</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<ColdEffect>())
        {
            if (insect.freezeInternalCooldown <= 0)
            {
                insect.RemoveEffect<WetEffect>();
                insect.RemoveEffect<ColdEffect>();
                insect.freezeInternalCooldown = 5f;
                insect.ApplyEffect(new FreezeEffect(insect, 4f, 1, source));
            }
        }
        else if (insect.HasEffect<TaintedEffect>())
        {
            insect.RemoveEffect<WetEffect>();
            insect.RemoveEffect<TaintedEffect>();
            insect.ApplyEffect(new SludgeEffect(insect, 4f, 1, source));
        }
        else if (insect.HasEffect<BlazeEffect>())
        {
            insect.RemoveEffect<WetEffect>();
            insect.RemoveEffect<BlazeEffect>();
            insect.ApplyEffect(new BoilEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<SproutEffect>())
        {
            if (insect.germinateInternalCooldown <= 0)
            {
                insect.RemoveEffect<WetEffect>();
                insect.RemoveEffect<SproutEffect>();
                insect.germinateInternalCooldown = 2f;
                insect.ApplyEffect(new GerminateEffect(insect, 2f, 1, source));
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
