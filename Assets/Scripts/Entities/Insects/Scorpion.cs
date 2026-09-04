using UnityEngine;

public class Scorpion : Insect
{
    public float venomDPS      = 16f;
    public float venomDuration = 12f;

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
        bool hit = current.ReceiveAttack(attackDamage, this);
        Entity victim = current as Entity;
        if (hit && victim != null && current.IsAlive)
            victim.ApplyEffect(new VenomEffect(victim, venomDuration, 1, this, venomDPS));
    }

    public override string GetDescription() =>
        $"Mildly aggressive and bulky arachnid. Attacks apply Venom, dealing <color=green><b>{(SData?.venomDPS ?? venomDPS):F0}</b></color> " +
        $"<color=#FFB6C1>Magic</color> Damage per second for <b>{(SData?.venomDuration ?? venomDuration):F0}</b> seconds." + AggressivityLine();
}
