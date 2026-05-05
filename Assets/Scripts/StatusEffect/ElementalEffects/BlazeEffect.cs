using UnityEngine;

public class BlazeEffect : ElementalDebuff
{
    public BlazeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override void OnApply()
    {
        Debug.Log("Blaze inflicted");

        Insect insect = (Insect)target;
        if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<SproutEffect>();
            insect.ApplyEffect(new BurnEffect(insect, 5f, 1, source));

        } else if (insect.HasEffect<TaintedEffect>()) {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<TaintedEffect>();
            insect.ApplyEffect(new VulnerableEffect(insect, 8f, 1, source));
        }
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Blaze removed");
    }
}
