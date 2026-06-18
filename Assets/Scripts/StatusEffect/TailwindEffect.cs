// the Morning Glory's passive buff: faster attacks and projectiles for nearby plants.
// applied as multipliers, refreshed each aura tick (Begonia pattern)
public class TailwindEffect : StatusEffect
{
    public readonly float attackSpeedBonus;
    public readonly float projectileSpeedBonus;
    private const float AccuracyBonus = 0.40f;
    private bool _increasesAccuracy;

    public TailwindEffect(Entity target, float duration, int level, Entity source, float attackSpeedBonus, float projectileSpeedBonus)
        : base(target, duration, level, source)
    {
        this.attackSpeedBonus     = attackSpeedBonus;
        this.projectileSpeedBonus = projectileSpeedBonus;
        effectType      = Type.positive;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        target.attackSpeedMultiplier     += attackSpeedBonus;
        target.projectileSpeedMultiplier += projectileSpeedBonus;
        _increasesAccuracy = source is MorningGlory mg && mg.IsPath2Maxed;
        if (_increasesAccuracy) target.accuracyAdder += AccuracyBonus;
    }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier     -= attackSpeedBonus;
        target.projectileSpeedMultiplier -= projectileSpeedBonus;
        if (_increasesAccuracy) target.accuracyAdder -= AccuracyBonus;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#B2EBF2>Tailwind</color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>, <color=green><b>Projectile Speed</b></color> by <color=green><b>{projectileSpeedBonus * 100f:F0}%</b></color>";
        desc += _increasesAccuracy
            ? $", and <color=green><b>Accuracy</b></color> by <color=green><b>{AccuracyBonus * 100f:F0}%</b></color>."
            : ".";
        return desc;
    }
}
