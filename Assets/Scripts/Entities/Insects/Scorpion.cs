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
        targetingRange = 1.5f;
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
        Plant plant = current as Plant;
        if (plant != null && plant.IsAlive)
            plant.ApplyEffect(new VenomEffect(plant, venomDuration, 1, this, venomDPS));
    }
}
