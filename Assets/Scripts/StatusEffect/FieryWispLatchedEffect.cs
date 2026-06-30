// applied by a FieryWisp while latched. inherits the per-second regen tick from RegenerationEffect
// and also boosts the plant's fire damage while active
public class FieryWispLatchedEffect : RegenerationEffect
{
    private readonly float _fireDamageFrac;

    public FieryWispLatchedEffect(Entity target, float duration, int level, Entity source,
                                   float healingPerSecond, float fireDamageFrac, float tickInterval = 1f)
        : base(target, duration, level, source, healingPerSecond, tickInterval)
    {
        _fireDamageFrac = fireDamageFrac;
        elementalType = ElementalType.Fire;
    }

    public override void OnApply()
    {
        target.fireDamageAdder += _fireDamageFrac;
    }

    public override void OnExpire()
    {
        target.fireDamageAdder -= _fireDamageFrac;
    }

    public override void OnTargetDied()
    {
        target.fireDamageAdder -= _fireDamageFrac;
    }

    public override string GetName() => "<color=orange><b>Fiery Assistance</b></color>";
    public override string GetDescription() =>
        $"Recovering <color=orange><b>{healingPerSecond:F0}</b></color> health per second. " +
        $"<color=orange>Fire Damage</color> increased by <color=green><b>{_fireDamageFrac * 100f:F0}%</b></color>.";
}
