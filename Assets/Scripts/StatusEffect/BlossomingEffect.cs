using UnityEngine;

public class BlossomingEffect : StatusEffect
{
    private readonly float attackSpeedBonus;
    // Blooming Affinity/Vigor (skill tree): a snapshot of Begonia's own current Begonia's
    // Blessing bonus at cast time, granted directly to the Blossoming target regardless of
    // whether it's actually in her Blessing's range - zero when the matching unlock is off
    private readonly float elementalAffinityBonus;
    private readonly float grassDamageBonus;

    public const float GerminateRadiusMultiplier = 1.5f;
    public const int GerminateMaxTargetsBonus = 8;
    // while this plant carries Blossoming, dealing Water or Grass damage shaves this much off
    // that insect's own matching primer timer (see Entity.Damage's Water/Grass branches) - scales
    // with Begonia's Path3 level, computed by her and passed in like every other level-scaled
    // number on this effect, rather than living here as a flat constant
    public readonly float PrimerCooldownReduction;

    // read by GerminateEffect on construction, so a Germinate this plant causes snapshots the
    // radius bonus at the moment it's applied - it keeps the bigger radius even if Blossoming
    // itself expires before that Germinate detonates
    public bool GrantsGerminateRadiusBonus { get; private set; }

    // Rebloom (skill tree): guards against the reapplication below ever chaining more than once
    // off a single original cast
    private readonly bool _isRebloom;

    public BlossomingEffect(Entity target, float duration, int level, Entity source, float attackSpeedBonus, float primerCooldownReduction,
        float elementalAffinityBonus = 0f, float grassDamageBonus = 0f, bool isRebloom = false)
        : base(target, duration, level, source)
    {
        this.attackSpeedBonus = attackSpeedBonus;
        PrimerCooldownReduction = primerCooldownReduction;
        this.elementalAffinityBonus = elementalAffinityBonus;
        this.grassDamageBonus = grassDamageBonus;
        _isRebloom = isRebloom;
        effectType      = Type.positive;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Blossoming", new Color(0.31f, 0.76f, 0.97f));
        target.attackSpeedMultiplier += attackSpeedBonus;
        target.elementalAffinityAdder += elementalAffinityBonus;
        target.grassDamageAdder += grassDamageBonus;

        GrantsGerminateRadiusBonus = source is Begonia beg && beg.path3Level >= Plant.absoluteLevelCap;
    }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier -= attackSpeedBonus;
        target.elementalAffinityAdder -= elementalAffinityBonus;
        target.grassDamageAdder -= grassDamageBonus;

        // Rebloom: if the bearer is still within range when this lapses, it blooms again once
        // more at half duration, instead of just falling off
        if (!_isRebloom && source is Begonia beg && beg.IsAlive && SkillTreeManager.HasUnlock(beg, Begonia.RebloomUnlock)
            && Vector3.Distance(target.transform.position, beg.transform.position) <= beg.attackRange)
        {
            target.ApplyEffect(new BlossomingEffect(target, duration * 0.5f, level, source, attackSpeedBonus, PrimerCooldownReduction,
                elementalAffinityBonus, grassDamageBonus, isRebloom: true));
        }
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#4FC3F7>Blossoming</color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>. " +
            $"Dealing <color=#4FC3F7><b>Water</b></color> or <color=green><b>Grass</b></color> damage reduces that insect's own primer cooldown for that element by <color=green><b>{PrimerCooldownReduction:F1}s</b></color>.";
        if (elementalAffinityBonus > 0f)
            desc += $" Also grants <color=green><b>Elemental Affinity</b></color> +<color=green><b>{elementalAffinityBonus * 100f:F0}%</b></color>.";
        if (grassDamageBonus > 0f)
            desc += $" Also grants <color=green><b>Grass Damage</b></color> +<color=green><b>{grassDamageBonus * 100f:F0}%</b></color>.";
        if (GrantsGerminateRadiusBonus)
            desc += $" Also increases <color=green><b>Germinate</b></color> radius by <color=green><b>{(GerminateRadiusMultiplier - 1f) * 100f:F0}%</b></color> and its max targets to <color=green><b>{GerminateMaxTargetsBonus}</b></color>.";
        if (!_isRebloom && source is Begonia rebloomBeg && SkillTreeManager.HasUnlock(rebloomBeg, Begonia.RebloomUnlock))
            desc += " If still in range when this expires, it blooms again at half duration.";
        return desc;
    }
}
