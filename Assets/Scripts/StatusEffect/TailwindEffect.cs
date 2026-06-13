// the Morning Glory's passive buff: faster attacks and projectiles for nearby plants.
// applied as multipliers, refreshed each aura tick (Begonia pattern)
public class TailwindEffect : StatusEffect
{
    public readonly float attackSpeedBonus;
    public readonly float projectileSpeedBonus;

    public TailwindEffect(Entity target, float duration, int level, Entity source, float attackSpeedBonus, float projectileSpeedBonus)
        : base(target, duration, level, source)
    {
        this.attackSpeedBonus     = attackSpeedBonus;
        this.projectileSpeedBonus = projectileSpeedBonus;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.attackSpeedMultiplier     += attackSpeedBonus;
        target.projectileSpeedMultiplier += projectileSpeedBonus;
    }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier     -= attackSpeedBonus;
        target.projectileSpeedMultiplier -= projectileSpeedBonus;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#B2EBF2>Tailwind</color>";
    public override string GetDescription() =>
        $"Increase <color=green><b>Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color> and <color=green><b>Projectile Speed</b></color> by <color=green><b>{projectileSpeedBonus * 100f:F0}%</b></color>.";
}
