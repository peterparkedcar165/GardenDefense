// was bud. attack speed through the normal stat (StatType.AttackSpeed) and less damage through
// Plant.mulchDamageBonus, which every hit of this plant on an insect reads
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
        Owner.mulchDamageBonus -= penalty;
    }

    public override void Deactivate()
    {
        if (Owner != null)
        {
            PlantStatApplier.Apply(Owner, StatType.AttackSpeed, -speed);
            Owner.mulchDamageBonus += penalty;
        }
        speed = penalty = 0f;
    }
}
