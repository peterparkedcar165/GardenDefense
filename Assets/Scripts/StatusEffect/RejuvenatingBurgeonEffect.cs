using UnityEngine;

public class RejuvenatingBurgeonEffect : RegenerationEffect
{
    public RejuvenatingBurgeonEffect(Entity target, float duration, int level, Entity source, float healPerTick, float tickInterval)
        : base(target, duration, level, source,
               healingPerSecond: healPerTick,
               tickInterval: tickInterval)
    {
    }

    public override string GetName()        => "<color=green><b>Rejuvenating Burgeon</b></color>";
    public override string GetDescription() =>
        $"Recovering <color=green><b>{Mathf.RoundToInt(healingPerSecond)}</b></color> health every <color=green><b>{tickInterval}s</b></color>.";
}
