using UnityEngine;
using System.Collections.Generic;

// Nerium Oleander (Verdance): the ally-facing mirror of OleandicToxinEffect - instead of locking
// buffs off an afflicted insect, this locks debuffs off a protected ally plant. uses the generic
// StatusEffect.TryBlockNegativeEffect hook (Entity.ApplyEffect already calls this on every active
// effect before applying a negative one - same hook HelleboreProtectionEffect/TauntEffect use)
// rather than a bespoke Entity.cs special-case like the original Toxin needed.
// the heal on application is deliberately weak - the real value is the regeneration tick, whose
// strength scales with how many debuff types are currently locked out
public class OleandicWardEffect : StatusEffect
{
    // set of debuff types that have been captured and are now locked out
    public HashSet<System.Type> immuneTypes = new HashSet<System.Type>();
    // display names of each locked effect, in lock order
    public List<string> immuneNames = new List<string>();

    private readonly float baseDuration;
    private readonly float weakHealAmount;
    private readonly float regenPerLockPerSecond;
    private float regenTickTimer;
    private const float RegenTickInterval = 1f;

    // Path2 max level bonus: +Magic Resistance per debuff locked, synced the same way Toxin
    // synced its Magic Armor reduction per buff captured
    private float _magicResistanceBonus = 0f;
    private float MagicResistancePerLock => (source as NeriumOleander)?.OleanderData?.path2MaxMagicResistancePerLock ?? 0.05f;

    public OleandicWardEffect(Entity target, float duration, int level, Entity source,
                               float weakHealAmount, float regenPerLockPerSecond)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Poison;
        tags = new EffectTag[] { EffectTag.Regenerative };
        baseDuration = duration;
        this.weakHealAmount = weakHealAmount;
        this.regenPerLockPerSecond = regenPerLockPerSecond;
    }

    public override void OnApply()
    {
        target.Heal(weakHealAmount, source);
        CleanseRandomDebuff();
        SyncMagicResistanceBonus();
    }

    // called every aura tick while the ally stays in range (same shape as
    // SnowWardEffect.RefreshPresence) - only extends duration and keeps the source current.
    // deliberately does NOT re-heal or re-cleanse: those are one-time, on first application only
    // (see OnApply), otherwise re-triggering them every 0.5s aura tick would make the "weak" heal
    // anything but weak, and strip every debuff within a couple seconds
    public void RefreshPresence(Entity newSource)
    {
        source = newSource;
        duration = baseDuration;
    }

    // called from Entity.ApplyEffect for every negative effect about to land on this target -
    // returns true to block it outright
    public override bool TryBlockNegativeEffect(StatusEffect incoming)
    {
        System.Type t = incoming.GetType();
        if (immuneTypes.Contains(t)) return true; // already locked, silently block it
        string name = incoming.GetName();
        immuneTypes.Add(t);
        immuneNames.Add(name);
        SpawnLockIndicator(name);
        SyncMagicResistanceBonus();
        return true;
    }

    private void SyncMagicResistanceBonus()
    {
        if (!(source is NeriumOleander oleander) || !oleander.IsPath2Maxed) return;
        float expected = immuneTypes.Count * MagicResistancePerLock;
        float delta = expected - _magicResistanceBonus;
        if (delta <= 0f) return;
        target.magicResistanceAdder += delta;
        _magicResistanceBonus += delta;
    }

    private void CleanseRandomDebuff()
    {
        var negativeEffects = new List<StatusEffect>();
        foreach (StatusEffect effect in target.activeEffects)
            if (effect.effectType == StatusEffect.Type.negative) negativeEffects.Add(effect);
        if (negativeEffects.Count == 0) return;

        int idx = Random.Range(0, negativeEffects.Count);
        StatusEffect toRemove = negativeEffects[idx];
        System.Type t = toRemove.GetType();
        string name = toRemove.GetName();
        if (!immuneTypes.Contains(t))
        {
            immuneTypes.Add(t);
            immuneNames.Add(name);
        }
        // remove first, then expire, so OnExpire doesn't re-trigger removal logic
        target.activeEffects.Remove(toRemove);
        toRemove.OnExpire();
        SpawnLockIndicator(name);
    }

    private void SpawnLockIndicator(string debuffName) =>
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), $"Locked: {debuffName}", new Color(0.3f, 0.85f, 0.5f));

    // the regeneration, not the lockout itself, is what scales with lock count - 0 locks means
    // just the weak one-time heal from OnApply/RefreshAndStack
    public override void OnTick(float deltaTime)
    {
        if (immuneTypes.Count == 0) return;
        regenTickTimer += deltaTime;
        if (regenTickTimer < RegenTickInterval) return;
        regenTickTimer -= RegenTickInterval;
        HealTarget(regenPerLockPerSecond * immuneTypes.Count * RegenTickInterval);
    }

    public override void OnExpire()
    {
        if (_magicResistanceBonus > 0f)
        {
            target.magicResistanceAdder -= _magicResistanceBonus;
            _magicResistanceBonus = 0f;
        }
    }

    public override string GetName() => "<color=#2ECC71>Oleandic Ward</color>";
    public override string GetDescription()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Locks out debuffs, preventing them from being reapplied while the effect is active.");
        sb.AppendLine($"Regenerates <color=green><b>{regenPerLockPerSecond * immuneTypes.Count:F0}</b></color> Health per second.");

        if (source is NeriumOleander oleander && oleander.IsPath2Maxed && immuneTypes.Count > 0)
            sb.AppendLine($"Increase <color=#FF69B4><b>Magic Resistance</b></color> by <color=#FF69B4><b>{_magicResistanceBonus * 100f:F0}%</b></color>.");

        if (immuneNames.Count == 0)
        {
            sb.AppendLine("Waiting to lock a debuff.");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#2ECC71><b><u>Debuffs Locked:</u></b></color> [<b>{immuneNames.Count}</b>]");
            foreach (string name in immuneNames)
                sb.AppendLine($"[{name}]");
        }

        return sb.ToString().TrimEnd();
    }
}
