using UnityEngine;

public abstract class HardCrowdControl : StatusEffect
{
    public HardCrowdControl(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        // for stun, nothing occurs.
    }

    public override void OnApply()
    {
        // nothing special applies here neither
        Debug.Log("Hard cc applied");
    }

    public override void OnExpire()
    {
        Debug.Log("Hard cc expired");
    }
}
