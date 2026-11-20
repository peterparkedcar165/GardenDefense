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
}
