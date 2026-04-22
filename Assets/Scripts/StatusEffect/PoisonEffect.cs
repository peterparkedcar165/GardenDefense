using UnityEngine;

public class PoisonEffect : StatusEffect
{
    public float damagePerSecond;
    public PoisonEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
    }

    public override void OnApply()
    {
        damagePerSecond = 2 + level;
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
