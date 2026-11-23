using UnityEngine;

public class PoisonedEffect : DoTEffect, IElementalAffinityEffect
{
    private ParticleSystem poisonParticles;
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    private const float missingHealthPercent = 0.04f;
    private const float flatDamage = 4f;
    private float cachedElementalAffinity;
    // a spread from Plague Bearer carries the affinity of the poison it came from, and isn't a
    // reaction of its own (Botanical Alchemy pays its ticks, not its landing)
    private bool spread;

    public float AffinityPower => source?.elementalAffinity ?? 0f;
    public override bool IsReaction => !spread;

    public PoisonedEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source, 1f)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
    }

    public override string GetName() => "<color=purple>Poisoned</color>";
    public override string GetDescription() =>
        $"Deal <color=purple><b>{PerTickOn(target, cachedElementalAffinity) / ticks.baseInterval * (1f + ticks.tickSpeedBonus):F0}</b></color> <color=purple>Poison</color> <color=#FFB6C1>Magic</color> damage per second.";

    // one tick on t, "4% missing health + 4" scaled by elemental affinity (Tommy, 2026-11-23): the
    // flat part is fixed when it lands (the affinity is read then), the missing health part is read
    // live as each tick lands, so it still hits harder the more hurt the target already is
    private static float PerTickOn(Entity t, float affinity, float interval = 1f) =>
        ((t.maxHealth - t.health) * missingHealthPercent + flatDamage) * (1f + 0.33f * affinity) * interval;

    public float DamagePerTickOn(Entity t) => PerTickOn(t, cachedElementalAffinity, ticks.baseInterval);

    public override void OnApply()
    {
        if (!spread) cachedElementalAffinity = source?.elementalAffinity ?? 0f;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Poisoned", new Color(0.6f, 0.1f, 0.8f));
        GameObject fx = Object.Instantiate(Resources.Load<GameObject>("PoisonBubbles"), target.transform.position, Quaternion.identity);
        fx.transform.SetParent(target.transform);
        fx.transform.localPosition = Vector3.zero;
        poisonParticles = fx.GetComponent<ParticleSystem>();
    }

    protected override void DealTick(float share)
    {
        float damage = PerTickOn(target, cachedElementalAffinity, ticks.baseInterval) * share;
        if (source != null)
            target.Damage(damage, DamageType.Magic, ElementalType.Poison, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
        else
            target.Damage(damage, DamageType.Magic, ElementalType.Poison, tickTags);
        MulchManager.ReactionEvent(this, source, target.transform.position);
    }

    public override void OnExpire()
    {
        if (poisonParticles != null)
            Object.Destroy(poisonParticles.gameObject);
    }

    // Plague Bearer on its source: an insect dying while it has this passes part of what's left on
    public override void OnTargetDied()
    {
        MulchManager.PlagueBearerOf(source)?.Spread(this);
    }

    // Plague Bearer (Tommy, 2026-11-23): `passed` seconds of `from` on t, with the same source and the
    // dying poison's own numbers. a t already Poisoned keeps the stronger of the two, the one dealing
    // more per tick on it right now, and the durations add up; the total follows from that per tick
    // amount and the new duration (OverTimeTicker). false when t isn't there to take it
    public static bool SpreadTo(Insect t, PoisonedEffect from, float passed)
    {
        if (t == null || !t.IsAlive || from == null || passed <= 0f) return false;
        PoisonedEffect existing = t.GetEffect<PoisonedEffect>();
        float newTick = PerTickOn(t, from.cachedElementalAffinity, from.ticks.baseInterval);
        if (existing != null && existing.DamagePerTickOn(t) >= newTick)
        {
            existing.duration += passed;
            existing.alchemyPayouts = 0;
            return true;
        }
        PoisonedEffect poison = new PoisonedEffect(t, 0f, 1, from.source)
        {
            spread = true,
            cachedElementalAffinity = from.cachedElementalAffinity,
            durationIsFinal = true,
        };
        poison.duration = passed;
        if (existing != null)
        {
            poison.duration += existing.duration;
            poison.ticks.CarryFrom(existing.ticks);
            t.RemoveEffect<PoisonedEffect>();
        }
        t.ApplyEffect(poison);
        return true;
    }
}
