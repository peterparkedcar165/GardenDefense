using UnityEngine;

public class SproutEffect : ElementalDebuff
{
    public SproutEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override string GetName() => "<color=#32CD32>Sprout</color>";

    public override void OnApply()
    {
        Debug.Log("Sprout inflicted");

        Insect insect = (Insect)target;
        if (insect.HasEffect<BlazeEffect>())
        {
            insect.RemoveEffect<SproutEffect>();
            insect.RemoveEffect<BlazeEffect>();
            insect.ApplyEffect(new BurnEffect(insect, 5f, 1, source));
        }
        else if (insect.HasEffect<ColdEffect>())
        {
            insect.RemoveEffect<SproutEffect>();
            insect.RemoveEffect<ColdEffect>();
            insect.ApplyEffect(new BrittleEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<TaintedEffect>())
        {
            insect.RemoveEffect<SproutEffect>();
            insect.RemoveEffect<TaintedEffect>();
            insect.ApplyEffect(new DecayEffect(insect, 8f, 1, source));
        } else if (insect.HasEffect<WetEffect>())
        {
            if (insect.germinateInternalCooldown <= 0)
            {
                insect.RemoveEffect<SproutEffect>();
                insect.RemoveEffect<WetEffect>();
                insect.germinateInternalCooldown = 4f;
                insect.ApplyEffect(new GerminateEffect(insect, 2f, 1, source));
            }
        }
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Sprout removed");
    }
}
