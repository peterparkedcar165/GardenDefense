using UnityEngine;

public class CaveSpider : Insect
{
    private const float webbedDuration = 3f;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity = Aggressivity.Medium;
    }

    public override void Attack()
    {
        IAttackable current = target;
        if (current == null) return;
        bool hit = current.ReceiveAttack(attackDamage, this);
        if (hit && current.IsAlive && current is Entity victim)
            victim.ApplyEffect(new WebbedEffect(victim, webbedDuration, 1, this));
    }
}
