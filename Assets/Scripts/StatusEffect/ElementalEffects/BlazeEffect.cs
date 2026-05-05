using UnityEngine;

public class BlazeEffect : ElementalDebuff
{
    public BlazeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override void OnApply()
    {
        Debug.Log("Blaze inflicted");
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Blaze removed");
    }
}
