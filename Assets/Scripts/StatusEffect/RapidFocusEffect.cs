public class RapidFocusEffect : StatusEffect
{
    private readonly float _speedMultiplier;

    public RapidFocusEffect(Entity target, float duration, int level, Entity source, float speedMultiplier)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        _speedMultiplier = speedMultiplier;
    }

    public override void OnApply()
    {
        target.attackSpeedMultiplier += _speedMultiplier;
    }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier -= _speedMultiplier;
    }

    public override string GetName() => "<color=green><b>Rapid Focus</b></color>";
    public override string GetDescription() => $"<color=green>Attack Speed</color> increased by <color=green><b>{_speedMultiplier * 100f:F0}%</b></color>.";
}
