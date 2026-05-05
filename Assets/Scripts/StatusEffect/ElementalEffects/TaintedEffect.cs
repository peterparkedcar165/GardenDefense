using UnityEngine;

public class TaintedEffect : ElementalDebuff
{
    public TaintedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override void OnApply()
    {
        Debug.Log("Tainted inflicted");
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        Debug.Log("Tainted removed");
    }
}
