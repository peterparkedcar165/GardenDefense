public class BegoniaBlessingEffect : PlantAuraBuffEffect
{
    public readonly float elementalAffinityBonus;
    public readonly float grassDamageBonus;
    public readonly bool grantsGerminateCrit;

    // the amounts actually granted in OnApply, after any Blooming Affinity/Vigor amplification -
    // stored so OnExpire removes exactly what was added, even though that can be more than the
    // raw elementalAffinityBonus/grassDamageBonus fields above if the target was Blossoming at the time
    private float _appliedAffinityBonus;
    private float _appliedGrassBonus;

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
        bool blooming = target.HasEffect<BlossomingEffect>();
        bool amplifyAffinity = blooming && source is Begonia affBeg && SkillTreeManager.HasUnlock(affBeg, Begonia.BloomingAffinityUnlock);
        bool amplifyGrass    = blooming && source is Begonia vigBeg && SkillTreeManager.HasUnlock(vigBeg, Begonia.BloomingVigorUnlock);

        _appliedAffinityBonus = amplifyAffinity ? elementalAffinityBonus * 1.5f : elementalAffinityBonus;
        _appliedGrassBonus    = amplifyGrass    ? grassDamageBonus * 1.5f      : grassDamageBonus;

        target.elementalAffinityAdder += _appliedAffinityBonus;
        target.grassDamageAdder += _appliedGrassBonus;
        if (grantsGerminateCrit) target.AddElementalReactionCanCrit();
    }

    public override void OnExpire()
    {
        target.elementalAffinityAdder -= _appliedAffinityBonus;
        target.grassDamageAdder -= _appliedGrassBonus;
        if (grantsGerminateCrit) target.RemoveElementalReactionCanCrit();
    }

    public override string GetName() => "<color=#4FC3F7><b>Begonia's Blessing</b></color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Elemental Affinity</b></color> by <color=green><b>{_appliedAffinityBonus * 100f:F0}%</b></color> and <color=green><b>Grass Damage</b></color> by <color=green><b>{_appliedGrassBonus * 100f:F0}%</b></color>.";
        if (grantsGerminateCrit) desc += " Also allows <color=green><b>Germinate</b></color> triggers to critically strike.";
        return desc;
    }
}
