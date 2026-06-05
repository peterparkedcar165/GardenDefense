using UnityEngine;

public class DesertLocust : Insect
{
    private const float devourDuration = 12f;
    private DesertLocustData LData => data as DesertLocustData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
    }

    public override void Attack()
    {
        // save target before base.Attack() resolves it (plant may die from the hit)
        IAttackable currentTarget = target;
        base.Attack();

        // a blinded locust can't latch on to devour. devours plants, or enemy insects while hypnotized
        if (currentTarget is Entity victim && currentTarget.IsAlive && !HasEffect<BlindEffect>())
            ApplyDevour(victim);
    }

    private void ApplyDevour(Entity victim)
    {
        DevourEffect existing = victim.GetEffect<DevourEffect>();
        if (existing == null)
        {
            victim.ApplyEffect(new DevourEffect(victim, devourDuration, 1, this));
            existing = victim.GetEffect<DevourEffect>();
        }
        existing?.AddStack(attackDamage * (LData?.devourReductionPercent ?? 1f));
    }
}
