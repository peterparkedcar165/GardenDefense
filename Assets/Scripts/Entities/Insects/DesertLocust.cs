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
        IAttackable current = target;
        if (current == null) return;
        bool hit = current.ReceiveAttack(attackDamage, this);
        if (hit && current is Entity victim && current.IsAlive)
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

    public override string GetDescription() =>
        $"Aggressive insect. Attacks inflict Devour, which reduces the target's Max Health equal to " +
        $"<b>{(LData?.devourReductionPercent ?? 1f) * 100f:F0}%</b> of the attack's damage, for <color=green><b>{devourDuration:F0}</b></color> seconds." + AggressivityLine();
}
