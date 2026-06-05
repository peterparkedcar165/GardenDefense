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
        current.ReceiveAttack(attackDamage, this);
        // a blinded spider can't aim its web. webs whatever it hits (plant, or enemy insect while hypnotized)
        if (current.IsAlive && current is Entity victim && !HasEffect<BlindEffect>())
            victim.ApplyEffect(new WebbedEffect(victim, webbedDuration, 1, this));
    }
}
