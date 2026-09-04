using UnityEngine;

// Bird of Paradise's skill: a timed self-buff granting Total Attack Speed, and (checked directly
// by BirdOfParadise.ExtraTargetCount via HasEffect<ThreeTalonStrikeEffect>) letting attacks hit
// 2 additional nearest enemies for the exact same effect while it's active
public class ThreeTalonStrikeEffect : StatusEffect
{
    private readonly float attackSpeedBonus;

    public ThreeTalonStrikeEffect(Entity target, float duration, int level, Entity source, float attackSpeedBonus)
        : base(target, duration, level, source)
    {
        this.attackSpeedBonus = attackSpeedBonus;
        effectType    = Type.positive;
        elementalType = ElementalType.Wind;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Three Talon Strike", new Color(0.6f, 1f, 0.9f));
        target.attackSpeedTotalMultiplier += attackSpeedBonus;
    }

    public override void OnExpire() => target.attackSpeedTotalMultiplier -= attackSpeedBonus;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#B2EBF2><b>Three Talon Strike</b></color>";
    public override string GetDescription() =>
        $"Increase <color=green><b>Total Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>, and attacks hit <color=green><b>2</b></color> additional nearest enemies for the exact same effect.";
}
