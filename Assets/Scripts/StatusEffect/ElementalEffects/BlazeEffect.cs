using UnityEngine;

public class BlazeEffect : StatusEffect
{
    private float tickTimer = 0f;
    public BlazeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        
    }
}
