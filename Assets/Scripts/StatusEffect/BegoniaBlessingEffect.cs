public class BegoniaBlessingEffect : PlantAuraBuffEffect
{
    public readonly float elementalAffinityBonus;
    public readonly float grassDamageBonus;
    public readonly bool grantsGerminateCrit;

    public BegoniaBlessingEffect(Entity target, int level, Plant source, float range, float elementalAffinityBonus, float grassDamageBonus, bool grantsGerminateCrit = false)
        : base(target, level, source, range)
    {
        this.elementalAffinityBonus = elementalAffinityBonus;
        this.grassDamageBonus = grassDamageBonus;
        this.grantsGerminateCrit = grantsGerminateCrit;
        effectType      = Type.positive;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        target.elementalAffinityAdder += elementalAffinityBonus;
        target.grassDamageAdder += grassDamageBonus;
        if (grantsGerminateCrit) target.AddElementalReactionCanCrit();
    }

    public override void OnExpire()
    {
        target.elementalAffinityAdder -= elementalAffinityBonus;
        target.grassDamageAdder -= grassDamageBonus;
        if (grantsGerminateCrit) target.RemoveElementalReactionCanCrit();
    }

    public override string GetName() => "<color=#4FC3F7><b>Begonia's Blessing</b></color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Elemental Affinity</b></color> by <color=green><b>{elementalAffinityBonus * 100f:F0}%</b></color> and <color=green><b>Grass Damage</b></color> by <color=green><b>{grassDamageBonus * 100f:F0}%</b></color>.";
        if (grantsGerminateCrit) desc += " Also allows <color=green><b>Germinate</b></color> triggers to critically strike.";
        return desc;
    }
}
