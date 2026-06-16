public class RejuvenatingBurgeonEffect : RegenerationEffect
{
    public RejuvenatingBurgeonEffect(Entity target, float duration, int level, Entity source, float totalHeal, float tickInterval)
        : base(target, duration, level, source,
               healingPerSecond: totalHeal / UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(duration / tickInterval)),
               tickInterval: tickInterval)
    {
    }

    public override string GetName()        => "<color=green><b>Rejuvenating Burgeon</b></color>";
    public override string GetDescription() =>
        $"Recovering <color=green><b>{healingPerSecond:F1}</b></color> health every <color=green><b>{tickInterval}s</b></color>.";
}
