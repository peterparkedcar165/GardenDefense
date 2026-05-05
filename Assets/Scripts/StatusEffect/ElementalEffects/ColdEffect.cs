using UnityEngine;

public class ColdEffect : ElementalDebuff
{
    public ColdEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override void OnApply()
    {
        Debug.Log("Cold inflicted");
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Cold removed");
    }
}
