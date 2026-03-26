using UnityEngine;

public class StunEffect : StatusEffect
{
    public Type effectType = Type.negative;
    public StunEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        // for stun, nothing occurs.
    }

    public override void OnApply()
    {
        // nothing special applies here neither
        Debug.Log("Stun applied");
    }

    public override void OnExpire()
    {
        Debug.Log("Stun expired");
    }
}
