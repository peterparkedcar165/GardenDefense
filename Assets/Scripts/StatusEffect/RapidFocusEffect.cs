public class RapidFocusEffect : StatusEffect
{
    private readonly float _speedMultiplier;

    // Focused Strikes / Verdant Focus (skill tree): both read straight off the source plant's
    // own unlocks at apply time - only ever constructed by LeafRanger on himself, so source is
    // always the same plant as target
    private const float FocusedStrikesCritChanceBonus = 0.15f;
    private const float FocusedStrikesCritDamageBonus = 0.20f;
    private const float VerdantFocusGrassDamageBonus = 0.20f;
    private const float VerdantFocusElementalAffinityBonus = 0.20f;
    private readonly bool _grantsFocusedStrikes;
    private readonly bool _grantsVerdantFocus;

    public RapidFocusEffect(Entity target, float duration, int level, Entity source, float speedMultiplier)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        _speedMultiplier = speedMultiplier;
        _grantsFocusedStrikes = source is Plant focusedPlant && SkillTreeManager.HasUnlock(focusedPlant, LeafRanger.FocusedStrikesUnlock);
        _grantsVerdantFocus   = source is Plant verdantPlant && SkillTreeManager.HasUnlock(verdantPlant, LeafRanger.VerdantFocusUnlock);
    }

    public override void OnApply()
    {
        target.attackSpeedMultiplier += _speedMultiplier;
        if (_grantsFocusedStrikes)
        {
            target.criticalChanceAdder += FocusedStrikesCritChanceBonus;
            target.criticalDamageAdder += FocusedStrikesCritDamageBonus;
        }
        if (_grantsVerdantFocus)
        {
            target.grassDamageAdder += VerdantFocusGrassDamageBonus;
            target.elementalAffinityAdder += VerdantFocusElementalAffinityBonus;
        }
    }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier -= _speedMultiplier;
        if (_grantsFocusedStrikes)
        {
            target.criticalChanceAdder -= FocusedStrikesCritChanceBonus;
            target.criticalDamageAdder -= FocusedStrikesCritDamageBonus;
        }
        if (_grantsVerdantFocus)
        {
            target.grassDamageAdder -= VerdantFocusGrassDamageBonus;
            target.elementalAffinityAdder -= VerdantFocusElementalAffinityBonus;
        }
    }

    public override string GetName() => "<color=green><b>Rapid Focus</b></color>";
    public override string GetDescription()
    {
        string desc = $"<color=green>Attack Speed</color> increased by <color=green><b>{_speedMultiplier * 100f:F0}%</b></color>.";
        if (_grantsFocusedStrikes)
            desc += $" <color=green>Critical Chance</color> increased by <color=green><b>{FocusedStrikesCritChanceBonus * 100f:F0}%</b></color>, and <color=green>Critical Damage</color> by <color=green><b>{FocusedStrikesCritDamageBonus * 100f:F0}%</b></color>.";
        if (_grantsVerdantFocus)
            desc += $" <color=green>Grass Damage</color> and <color=green>Elemental Affinity</color> increased by <color=green><b>{VerdantFocusGrassDamageBonus * 100f:F0}%</b></color>.";
        return desc;
    }
}
