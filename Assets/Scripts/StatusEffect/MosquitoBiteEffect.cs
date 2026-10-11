using UnityEngine;

// applied by Mosquito's bite to the plant it attacks - deals flat True damage per second
// (bypassing armor/magic armor entirely) and reduces the plant's incoming healing while
// active. Mosquito's own self-heal (Bloodsucker) is a separate effect applied directly on
// the Mosquito alongside this one in Mosquito.Attack, not managed from here
public class MosquitoBiteEffect : StatusEffect
{
    private readonly float damagePerSecond;
    private readonly float healingReductionPercent;
    private const float tickInterval = 1f;
    private float tickTimer = 0f;

    public MosquitoBiteEffect(Entity target, float duration, int level, Entity source, float damagePerSecond, float healingReductionPercent)
        : base(target, duration, level, source)
    {
        this.damagePerSecond = damagePerSecond;
        this.healingReductionPercent = healingReductionPercent;
        effectType = Type.negative;
        elementalType = ElementalType.Grass;
        tags = new EffectTag[] { EffectTag.DoT };
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Mosquito Bite", new Color(0.55f, 0.05f, 0.05f));
        target.healingReceivedAdder -= healingReductionPercent;
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer < tickInterval) return;
        tickTimer -= tickInterval;
        target.Damage(damagePerSecond * tickInterval, DamageType.True, ElementalType.Grass, source, source.DotCanCrit,
            new DamageTag[] { DamageTag.DoT });
    }

    public override void OnExpire()
    {
        target.healingReceivedAdder += healingReductionPercent;
    }

    public override string GetName() => "<color=#8B0000>Mosquito Bite</color>";
    public override string GetDescription() =>
        $"Deals <color=green><b>{damagePerSecond:F0}</b></color> <color=#6B8E23>Grass</color> True damage per second and reduces incoming healing by <color=red><b>{healingReductionPercent * 100f:F0}%</b></color>.";
}
