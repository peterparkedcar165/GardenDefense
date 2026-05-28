using UnityEngine;

public class SludgeEffect : StatusEffect
{
    private float bonusDuration;

    public SludgeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        bonusDuration = 4f * (1f + source.elementalAffinity);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#9400D3>Sludge</color>";
    public override string GetDescription() => $"Increase duration of DoT effects by <color=green><b>{bonusDuration:F1}s</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Sludge", new Color(0.6f, 0.1f, 0.8f));

        bool hasDoT = false;
        foreach (StatusEffect effect in target.activeEffects)
        {
            if (effect is DoTEffect)
            {
                effect.duration += bonusDuration;
                hasDoT = true;
            }
        }

        if (hasDoT)
        {
            target.RemoveEffect<SludgeEffect>();
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
