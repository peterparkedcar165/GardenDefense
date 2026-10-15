using UnityEngine;

// applied to every plant while the level's weather includes Sandstorm. reduces Accuracy by
// 10% per intensity level and deals 12 + 6 per intensity level damage per second - Aloe Vera and Cactus are
// desert-adapted and immune to both, and any plant currently carrying a Holly's Snow Ward
// (granted by her passive or her Blizzard, at 1.5x power) is also immune for as long as that
// holds - the immunity is SnowWardEffect's own doing, this just checks for its presence.
// immunity is re-checked every tick rather than baked in at construction, since Snow Ward can be
// granted or lost well after Sandstorm is already active
public class SandstormExposedEffect : StatusEffect
{
    private const float PenaltyPerLevel = 0.1f;
    private const float BaseDamagePerSecond = 4f;
    private const float DamagePerSecondPerLevel = 4f;
    private const float DamageTickInterval = 1f;

    private readonly float basePenalty;
    private readonly float damagePerSecond;
    private bool wasImmune;
    private float damageTickTimer;

    private static readonly DamageTag[] sandstormDamageTags = { DamageTag.Weather, DamageTag.DoT };

    public SandstormExposedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.negative;
        basePenalty = PenaltyPerLevel * level;
        damagePerSecond = BaseDamagePerSecond + DamagePerSecondPerLevel * level;
    }

    private bool IsImmune() =>
        target is AloeVera || target is Cactus || target.HasEffect<SnowWardEffect>();

    public override void OnApply()
    {
        wasImmune = IsImmune();
        if (!wasImmune) target.accuracyAdder -= basePenalty;
    }

    public override void OnExpire()
    {
        if (!wasImmune) target.accuracyAdder += basePenalty;
    }

    public override void OnTick(float deltaTime)
    {
        bool immuneNow = IsImmune();
        if (immuneNow != wasImmune)
        {
            target.accuracyAdder += immuneNow ? basePenalty : -basePenalty;
            wasImmune = immuneNow;
        }

        if (immuneNow) return;

        damageTickTimer += deltaTime;
        if (damageTickTimer < DamageTickInterval) return;
        damageTickTimer -= DamageTickInterval;

        float dmg = damagePerSecond * DamageTickInterval;
        target.Damage(dmg, DamageType.True, ElementalType.Neutral, sandstormDamageTags);
        // the no-source Damage() overload doesn't spawn one on its own (unlike the sourced
        // overload) - same manual spawn other weather/environmental damage (e.g. temperature) uses
        DamageIndicator.Spawn(target.transform.position + Vector3.up * 0.25f, dmg, ElementalType.Neutral, false);
    }

    public override string GetName() => "<color=#E8D9A8>[Weather]: Sandstorm</color>";
    public override string GetDescription() =>
        $"Reduce <color=red><b>Accuracy</b></color> by <color=red><b>{basePenalty * 100f:F0}%</b></color> and deal <color=red><b>{damagePerSecond:F0}</b></color> damage per second.";
}
