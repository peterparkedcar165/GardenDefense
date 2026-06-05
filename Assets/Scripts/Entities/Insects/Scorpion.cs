using UnityEngine;

public class Scorpion : Insect
{
    public float venomDPS      = 5f;
    public float venomDuration = 4f;

    private ScorpionData SData => data as ScorpionData;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        aggressivity   = Aggressivity.Medium;
        if (SData != null)
        {
            venomDPS      = SData.venomDPS;
            venomDuration = SData.venomDuration;
        }
    }

    public override void Attack()
    {
        IAttackable current = target;
        if (current == null) return;
        current.ReceiveAttack(attackDamage, this);
        // a blinded scorpion can't land its venomous sting. envenoms plants, or enemy insects while hypnotized
        Entity victim = current as Entity;
        if (victim != null && current.IsAlive && !HasEffect<BlindEffect>())
            victim.ApplyEffect(new VenomEffect(victim, venomDuration, 1, this, venomDPS));
    }
}
