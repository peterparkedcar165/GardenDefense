using UnityEngine;

public class TaintedEffect : ElementalDebuff
{
    public TaintedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override string GetName() => "<color=#8B008B>Tainted</color>";

    public override void OnApply()
    {
        Debug.Log("Tainted inflicted");

        Insect insect = (Insect)target;
        if (insect.HasEffect<BlazeEffect>())
        {
            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<BlazeEffect>();
            insect.ApplyEffect(new VulnerableEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<SproutEffect>();
            insect.ApplyEffect(new DecayEffect(insect, 8f, 1, source));
        } else if (insect.HasEffect<WetEffect>()) {

            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<WetEffect>();
            insect.ApplyEffect(new SludgeEffect(insect, 4f, 1, source));

        }
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Tainted removed");
    }
}
