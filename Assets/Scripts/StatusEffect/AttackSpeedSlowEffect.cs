using UnityEngine;

// reduces the target's attack speed by a percentage for the duration. re-applying refreshes
// rather than stacks (ApplyEffect replaces the existing one of the same type)
public class AttackSpeedSlowEffect : StatusEffect
{
    private readonly float percent;

    public AttackSpeedSlowEffect(Entity target, float duration, int level, Entity source, float percent)
        : base(target, duration, level, source)
    {
        this.percent = percent;
        effectType = Type.negative;
    }

    public override void OnApply()  => target.attackSpeedMultiplier -= percent;
    public override void OnExpire() => target.attackSpeedMultiplier += percent;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#9FD8E0>Sluggish</color>";
    public override string GetDescription() => $"<color=#9FD8E0><b>Attack Speed</b></color> reduced by <color=red><b>{percent * 100f:F0}%</b></color>.";
}
