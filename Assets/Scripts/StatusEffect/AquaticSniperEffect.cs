public class AquaticSniperEffect : ChannelingEffect
{
    private readonly float _attackSpeedBonus;
    private readonly float _critChanceBonus;

    public AquaticSniperEffect(Entity target, float duration, float attackSpeedBonus, float critChanceBonus, Entity source)
        : base(target, duration, 1, source)
    {
        _attackSpeedBonus = attackSpeedBonus;
        _critChanceBonus  = critChanceBonus;
        effectType        = Type.positive;
        elementalType     = ElementalType.Water;   // override ChannelingEffect's neutral
    }

    public override void OnApply()
    {
        target.attackSpeedMultiplier += _attackSpeedBonus;
        target.criticalChanceAdder   += _critChanceBonus;
        target.UpdateStats();
    }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier -= _attackSpeedBonus;
        target.criticalChanceAdder   -= _critChanceBonus;
        target.UpdateStats();
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#4FC3F7><b>Aquatic Sniper</b></color>";
    public override string GetDescription()
    {
        string base_ = $"Gaining <color=green><b>+{_attackSpeedBonus * 100f:F0}%</b></color> <color=green><b>Attack Speed</b></color> and <color=green><b>+{_critChanceBonus * 100f:F0}%</b></color> <color=#FFD700><b>Critical Chance</b></color>.";
        Cattail cattail = target as Cattail;
        if (cattail != null && cattail.IsPath3Maxed)
        {
            base_ += $"\nEach hit increases <color=#4FC3F7><b>Water Damage</b></color> by <color=green><b>1%</b></color>.";
            if (cattail.SkillWaterBonus > 0f)
                base_ += $" [<color=green><b>+{cattail.SkillWaterBonus * 100f:F0}%</b></color>]";
        }
        return base_;
    }
}
