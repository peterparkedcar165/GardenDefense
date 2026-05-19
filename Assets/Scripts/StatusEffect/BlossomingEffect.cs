public class BlossomingEffect : StatusEffect
{
    private readonly float natureDamageBonus;
    private readonly float attackSpeedBonus;

    public BlossomingEffect(Entity target, float duration, int level, Entity source, float natureDamageBonus, float attackSpeedBonus)
        : base(target, duration, level, source)
    {
        this.natureDamageBonus = natureDamageBonus;
        this.attackSpeedBonus = attackSpeedBonus;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.natureDamageAdder += natureDamageBonus;
        target.attackSpeedMultiplier += attackSpeedBonus;
    }

    public override void OnExpire()
    {
        target.natureDamageAdder -= natureDamageBonus;
        target.attackSpeedMultiplier -= attackSpeedBonus;
    }

    public override string GetName() => "<color=green>Blossoming</color>";
    public override string GetDescription() =>
        $"Nature Power increased by <color=green><b>{natureDamageBonus * 100f:F0}%</b></color>. " +
        $"Attack Speed increased by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>.";
}
