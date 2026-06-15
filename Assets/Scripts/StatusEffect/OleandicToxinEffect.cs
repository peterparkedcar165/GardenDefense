using UnityEngine;
using System.Collections.Generic;

public class OleandicToxinEffect : StatusEffect
{
    // set of buff types that have been captured and are now immune
    public HashSet<System.Type> immuneTypes = new HashSet<System.Type>();
    // display names of each captured effect, in capture order
    public List<string> immuneNames = new List<string>();
    private float baseDuration;
    private int _magicArmorReduction = 0;
    private const float MagicArmorPerLock = 8f;

    public OleandicToxinEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        baseDuration = duration;
    }

    public override void OnApply()
    {
        CleanseRandomBuff();
        SyncMagicArmorReduction();
    }

    // called on reapplication instead of creating a new instance
    // refreshes duration and triggers another cleanse
    public void RefreshAndStack(Entity newSource)
    {
        source = newSource;
        duration = baseDuration;
        CleanseRandomBuff();
        SyncMagicArmorReduction();
    }

    // called from Entity.ApplyEffect when a positive effect is being applied to this target
    // returns true if the incoming effect should be blocked
    public bool TryCapture(StatusEffect incoming)
    {
        System.Type t = incoming.GetType();
        if (immuneTypes.Contains(t)) return true; // already captured, silently block it
        // only capture a new buff type if none has been captured yet (waiting state)
        // once one is held, new types pass through; only another oleandic toxin hit can capture more
        if (immuneTypes.Count > 0) return false;
        string name = incoming.GetName();
        immuneTypes.Add(t);
        immuneNames.Add(name);
        SpawnCaptureIndicator(name, 0f);
        SyncMagicArmorReduction();
        return true;
    }

    private void CleanseRandomBuff()
    {
        var positiveEffects = new List<StatusEffect>();
        foreach (StatusEffect effect in target.activeEffects)
        {
            if (effect.effectType == StatusEffect.Type.positive)
                positiveEffects.Add(effect);
        }
        if (positiveEffects.Count == 0) return;

        int idx = Random.Range(0, positiveEffects.Count);
        StatusEffect toRemove = positiveEffects[idx];
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
        SpawnCaptureIndicator(name, 0.3f);
    }

    private void SpawnCaptureIndicator(string buffName, float yOffset)
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, yOffset, 0f), $"Captured: {buffName}", new Color(0.6f, 0.2f, 0.8f));
    }

    private void SyncMagicArmorReduction()
    {
        if (!(source is NeriumOleander oleander) || !oleander.IsPath2Maxed) return;
        int expected = (int)(immuneTypes.Count * MagicArmorPerLock);
        int delta = expected - _magicArmorReduction;
        if (delta <= 0) return;
        target.magicArmorAdder -= delta;
        _magicArmorReduction += delta;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        if (_magicArmorReduction > 0)
        {
            target.magicArmorAdder += _magicArmorReduction;
            _magicArmorReduction = 0;
        }
    }

    public override string GetName() => "<color=#9B59B6>Oleandic Toxin</color>";
    public override string GetDescription()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Cleanses a random buff, and prevents them from receiving that buff while the effect is active.");

        if (source is NeriumOleander oleander && oleander.IsPath2Maxed && immuneTypes.Count > 0)
            sb.AppendLine($"Decrease <color=#FF69B4><b>Magic Armor</b></color> by <color=#FF69B4><b>{immuneTypes.Count * (int)MagicArmorPerLock}</b></color>.");

        if (immuneNames.Count == 0)
        {
            sb.AppendLine("Waiting to capture a buff.");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#9B59B6><b><u>Buffs Captured:</u></b></color> [<b>{immuneNames.Count}</b>]");
            foreach (string name in immuneNames)
                sb.AppendLine($"[{name}]");
        }

        return sb.ToString().TrimEnd();
    }
}
