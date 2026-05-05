using UnityEngine;

public class SproutEffect : ElementalDebuff
{
    public SproutEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override void OnApply()
    {
        Debug.Log("Sprout inflicted");
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Sprout removed");
    }
}
