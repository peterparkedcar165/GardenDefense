using UnityEngine;

public class TansyGlowEffect : StatusEffect
{
    private readonly Tansy tansy;
    private float cachedLightRange;
    private float healTickTimer;
    private const float HealInterval = 0.5f;
    // flat + percent max health healed per second - scaled by HealInterval at each tick so
    // ticking more often (smoother health bar movement) doesn't change the actual heal rate
    private const float HealFlat = 6f;
    private const float HealPercent = 0.03f;

    public TansyGlowEffect(Entity target, float duration, int level, Entity source, Tansy tansy)
        : base(target, duration, level, source)
    {
        this.tansy  = tansy;
        effectType      = Type.positive;
        elementalType   = ElementalType.Fire;
        sourceStackable = true;
    }

    private float BestRangeExcluding(TansyGlowEffect exclude)
    {
        float best = 0f;
        foreach (var e in target.activeEffects)
            if (e is TansyGlowEffect fg && fg != exclude && fg.cachedLightRange > best)
                best = fg.cachedLightRange;
        return best;
    }

    public override void OnApply()
    {
        Plant plant = target as Plant;
        if (plant == null) return;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Floral Glow", new Color(1f, 0.6f, 0f));

        float prevBest = BestRangeExcluding(this);
        cachedLightRange = tansy?.baseLightEmissionRange ?? 0f;
        float delta = Mathf.Max(prevBest, cachedLightRange) - prevBest;
        if (delta > 0f)
        {
            plant.lightEmissionRangeAdder += delta;
            plant.UpdateStats();
        }
    }

    // Nurturing Glow: while active, Floral Glow also sustains its target with a flat + percent heal
    public override void OnTick(float deltaTime)
    {
        if (tansy == null || !tansy.NurturingGlowActive) return;
        Plant plant = target as Plant;
        if (plant == null || !plant.IsAlive) return;

        healTickTimer += deltaTime;
        if (healTickTimer < HealInterval) return;
        healTickTimer -= HealInterval;
        plant.Heal((HealFlat + plant.maxHealth * HealPercent) * HealInterval, tansy);
    }

    public override void OnExpire()
    {
        Plant plant = target as Plant;
        if (plant == null) return;

        float currentBest = Mathf.Max(BestRangeExcluding(this), cachedLightRange);
        float nextBest = BestRangeExcluding(this);
        float delta = currentBest - nextBest;
        if (delta > 0f)
        {
            plant.lightEmissionRangeAdder -= delta;
            plant.UpdateStats();
        }
    }

    public override string GetName() => "<color=orange>Floral Glow</color>";
    public override string GetDescription()
    {
        string desc = $"An orbiting petal projectile, sourced from the <color=orange><b>Tansy</b></color>, deals damage to anything it passes through.";
        if (tansy != null && tansy.NurturingGlowActive)
        {
            Plant plant = target as Plant;
            float heal = HealFlat + (plant?.maxHealth ?? 0f) * HealPercent;
            desc += $"\n\nRegenerates <color=green><b>{heal:F0}</b></color> Health per second.";
        }
        return desc;
    }
}
