using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// every Nth attack of its plant: each debuff that attack puts on an insect is applied a second time.
// an attack is counted from the plant's Attack tagged hits (Entity.OnHit), several hits in the same
// frame (a splash, a multi target attack) being one attack. debuffs the hit put on before or after
// its damage in that frame both count. primers aren't doubled, so reactions don't cascade
// (proposal: Tommy's rule is "applies its on hit effects twice"). the plant's other on hit effects
// (waterlily's splash, Plant.RepeatOnHitEffects) happen again repeatDelay seconds later
// (Tommy, 2026-11-22: splash twice, with a little delay)
public class EchoSeedMulchEffect : MulchEffect
{
    private readonly EchoSeedMulchData typedData;
    // the repeat's wait, made again only when the delay changes
    private WaitForSeconds repeatWait;
    private float repeatWaitDelay = -1f;
    private bool listening;
    private int attacks;
    // attacks it echoed this level
    private int echoes;
    private int lastAttackFrame = -1;
    private int echoFrame = -1;
    private bool echoing;
    // this frame's debuffs from this plant, per insect, and the insects this frame's echo covers
    private int recordFrame = -1;
    private readonly List<StatusEffect> recorded = new List<StatusEffect>();
    private readonly HashSet<Entity> echoTargets = new HashSet<Entity>();

    public EchoSeedMulchEffect(EchoSeedMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (listening) return;
        listening = true;
        Entity.OnHit += HandleHit;
        Entity.OnEffectApplied += HandleEffect;
    }

    public override void Deactivate()
    {
        if (!listening) return;
        listening = false;
        Entity.OnHit -= HandleHit;
        Entity.OnEffectApplied -= HandleEffect;
    }

    public override string GetStatus()
    {
        int every = Mathf.Max(1, VI(typedData.everyAttacks));
        int left = every - attacks % every;
        string next = left == every ? "Next attack: normal" : $"Echo in {left} {(left == 1 ? "attack" : "attacks")}";
        return $"{next}\nEchoed attacks: {echoes}";
    }

    private void HandleHit(EntityEventData hit)
    {
        if (!OwnerAlive || hit.source != Owner || hit.target is not Insect) return;
        int frame = Time.frameCount;
        if (frame != lastAttackFrame)
        {
            lastAttackFrame = frame;
            attacks++;
            int every = Mathf.Max(1, VI(typedData.everyAttacks));
            if (attacks % every == 0)
            {
                echoes++;
                echoFrame = frame;
                echoTargets.Clear();
            }
        }
        if (echoFrame != frame || !echoTargets.Add(hit.target)) return;

        // debuffs this hit already put on before its damage. no copy of the list: echoing is set
        // while a copy lands, so HandleEffect adds nothing to it, and only the debuffs that were
        // there when the loop began are echoed
        if (recordFrame == frame)
        {
            int count = recorded.Count;
            for (int i = 0; i < count && i < recorded.Count; i++)
            {
                StatusEffect effect = recorded[i];
                if (effect != null && effect.target == hit.target) Echo(effect);
            }
        }

        // the rest of its on hit effects (waterlily's splash) again, a moment later
        Insect insect = (Insect)hit.target;
        StartOnOwner(RepeatLater(insect, hit.position, insect.GetAimPoint()));
    }

    private IEnumerator RepeatLater(Insect target, Vector3 groundPoint, Vector3 aimPoint)
    {
        float delay = Mathf.Max(0f, typedData.repeatDelay);
        if (delay > 0f)
        {
            if (repeatWait == null || delay != repeatWaitDelay)
            {
                repeatWait = new WaitForSeconds(delay);
                repeatWaitDelay = delay;
            }
            yield return repeatWait;
        }
        if (!listening || !OwnerAlive) yield break;
        // the debuffs the repeat puts on are its own second application, not echoed again
        bool wasEchoing = echoing;
        echoing = true;
        try { Owner.RepeatOnHitEffects(target, groundPoint, aimPoint); }
        finally { echoing = wasEchoing; }
    }

    private void HandleEffect(StatusEffect effect)
    {
        if (echoing || !OwnerAlive || effect == null || effect.source != Owner) return;
        if (effect.target is not Insect || effect.effectType != StatusEffect.Type.negative) return;
        int frame = Time.frameCount;
        if (recordFrame != frame)
        {
            recordFrame = frame;
            recorded.Clear();
        }
        recorded.Add(effect);
        // debuffs the echo hit puts on after its damage
        if (echoFrame == frame && echoTargets.Contains(effect.target)) Echo(effect);
    }

    // a copy of the debuff applied again, so it stacks or refreshes like a second hit would
    // (the copy replacing the original runs the original's OnExpire, then the copy's OnApply,
    // exactly as a second hit's debuff would)
    private void Echo(StatusEffect effect)
    {
        if (effect.target == null || effect.target.health <= 0f) return;
        // a direct copy (no reflection), with its own tick timer (StatusEffect.CopyForEcho)
        StatusEffect copy = effect.CopyForEcho();
        // the copy's duration was already scaled by the duration stats when the original landed
        copy.durationIsFinal = true;
        // kept as it was, so a copy landing inside another copy's landing doesn't switch it off early
        bool wasEchoing = echoing;
        echoing = true;
        try { effect.target.ApplyEffect(copy); }
        finally { echoing = wasEchoing; }
    }
}
