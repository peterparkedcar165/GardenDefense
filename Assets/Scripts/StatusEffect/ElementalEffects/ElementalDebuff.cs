using UnityEngine;

public abstract class ElementalDebuff : StatusEffect
{ 
    public ElementalDebuff(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        Insect insect = (Insect)target;
    }

    public override void OnTick(float deltaTime)
    {
        
    }

    public override void OnExpire()
    {
        
    }
}
