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
        // a blinded spider can't aim its web
        if (current is Plant plant && plant.IsAlive && !HasEffect<BlindEffect>())
            plant.ApplyEffect(new WebbedEffect(plant, webbedDuration, 1, this));
    }
}
