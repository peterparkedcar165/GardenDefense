using UnityEngine;

public class PoisonEffect : StatusEffect
{
    public Type effectType = Type.negative;
    public float damagePerSecond;
    public PoisonEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        
    }

    public override void OnApply()
    {
        damagePerSecond = level;
        Debug.Log("Poison applied at level " + damagePerSecond);
    }

    public override void OnTick(float deltaTime)
    {
        Insect insect = (Insect)target;
        target.Damage((damagePerSecond * deltaTime) * (1 - insect.poisonResistance), DamageType.Magic);
    }

    public override void OnExpire()
    {
        Debug.Log("Poison expired");
    }
}
