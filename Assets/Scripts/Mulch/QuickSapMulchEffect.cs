using UnityEngine;

// was bud. attack speed through the normal stat (StatType.AttackSpeed), and its total Attack Damage
// lowered through StatType.AttackDamageTotalMultiplier (Tommy: "reduce Attack Damage by X%"), so
// damage that doesn't scale with Attack Damage isn't touched
public class QuickSapMulchEffect : MulchEffect
{
    private readonly QuickSapMulchData typedData;
    private float speed, penalty;

    public QuickSapMulchEffect(QuickSapMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        speed = V(typedData.attackSpeedBonus);
        penalty = V(typedData.damagePenalty);
        PlantStatApplier.Apply(Owner, StatType.AttackSpeed, speed);
        PlantStatApplier.Apply(Owner, StatType.AttackDamageTotalMultiplier, -penalty);
    }

    public override void Deactivate()
    {
        if (Owner != null)
        {
            PlantStatApplier.Apply(Owner, StatType.AttackSpeed, -speed);
            PlantStatApplier.Apply(Owner, StatType.AttackDamageTotalMultiplier, penalty);
        }
        speed = penalty = 0f;
    }

    // its plant's attack speed and Attack Damage now, and what they'd be without it (the same
    // formulas as Entity.UpdateStats with its part taken out)
    public override string GetStatus()
    {
        if (Owner == null) return null;
        float speedWithout = Owner.attackSpeed - Owner.baseAttackSpeed * speed * Owner.attackSpeedTotalMultiplier;
        float raw = Owner.baseAttackDamage + Owner.attackDamageAdder + Owner.baseAttackDamage * Owner.attackDamageMultiplier;
        float damageWithout = Mathf.Max(0f, raw * (Owner.attackDamageTotalMultiplier + penalty));
        return $"Attack Speed: {Owner.attackSpeed:0.00} ({speedWithout:0.00} without it)\n" +
            $"Attack Damage: {Owner.attackDamage:0} ({damageWithout:0} without it)";
    }
}
