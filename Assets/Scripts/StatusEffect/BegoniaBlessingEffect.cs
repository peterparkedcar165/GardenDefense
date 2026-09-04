public class BegoniaBlessingEffect : PlantAuraBuffEffect
{
    public readonly float critChanceBonus;
    public readonly float maxDamageBonus;
    public readonly float armorShredBonus;

    public BegoniaBlessingEffect(Entity target, int level, Plant source, float range, float critChanceBonus, float maxDamageBonus, float armorShredBonus = 0f)
        : base(target, level, source, range)
    {
        this.critChanceBonus = critChanceBonus;
        this.maxDamageBonus = maxDamageBonus;
        this.armorShredBonus = armorShredBonus;
        effectType      = Type.positive;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        target.criticalChanceAdder += critChanceBonus;
        target.maximumDamageAdder += maxDamageBonus;
        if (armorShredBonus > 0f) target.armorPenPercentAdder += armorShredBonus;
    }

    public override void OnExpire()
    {
        target.criticalChanceAdder -= critChanceBonus;
        target.maximumDamageAdder -= maxDamageBonus;
        if (armorShredBonus > 0f) target.armorPenPercentAdder -= armorShredBonus;
    }

    public override string GetName() => "<color=green><b>Begonia's Blessing</b></color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Critical Chance</b></color> by <color=green><b>{critChanceBonus * 100f:F0}%</b></color> and <color=green><b>Maximum Damage</b></color> by <color=green><b>{maxDamageBonus * 100f:F0}%</b></color>.";
        if (armorShredBonus > 0f) desc += $" Also increases <color=green><b>Armor Shred</b></color> by <color=green><b>{armorShredBonus * 100f:F0}%</b></color>.";
        return desc;
    }
}
