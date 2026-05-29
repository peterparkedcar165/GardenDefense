using UnityEngine;

public class FrostbiteEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public float healthPerSecond = 0.04f, adPerSecond = 0.06f;
    private float tenacityReduction = 0.33f;
    private float cachedelementalAffinity;
    private float cachedMaxHealth;
    private float cachedAttackDamage;

    public FrostbiteEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        cachedelementalAffinity = source?.elementalAffinity ?? 0f;
        effectType = Type.negative;
        tickInterval = 0.25f;
    }

    public override string GetName() => "<color=#00BFFF>Frostbite</color>";
    public override string GetDescription()
    {
        float hp = cachedMaxHealth > 0 ? cachedMaxHealth : (target?.maxHealth ?? 0f);
        float ad = cachedAttackDamage > 0 ? cachedAttackDamage : (source?.attackDamage ?? 0f);
        float ep = cachedelementalAffinity;
        float total = ((healthPerSecond * hp) + (adPerSecond * ad) + 2f) * (1f + ep);
        return $"Deal <color=#00BFFF><b>{total:F0}</b></color> <color=#00BFFF>Ice</color> Physical damage per second. " +
               $"(<color=red>{healthPerSecond * 100:F0}% Max Health</color> + <color=#B0B0FF>{adPerSecond * 100:F0}% Attack Damage</color> + 2) × " +
               $"(1 + <color=#FFD700>{ep * 100:F0}% Elemental Affinity</color>). " +
               $"Reduces Tenacity by <color=green>{tenacityReduction * 100:F0}%</color>.";
    }

    public override void OnApply()
    {
        base.OnApply();
        cachedMaxHealth    = target.maxHealth;
        cachedAttackDamage = source?.attackDamage ?? 0f;
        damagePerSecond = ((healthPerSecond * cachedMaxHealth) + (adPerSecond * cachedAttackDamage) + 2f) * (1f + cachedelementalAffinity);

        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Frostbite", new Color(0f, 1f, 1f));

        target.tenacityMultiplier -= tenacityReduction;
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
            if (source != null)
                target.Damage(damagePerSecond * tickInterval, DamageType.Physical, ElementalType.Ice, source, false, tickTags);
            else
                target.Damage(damagePerSecond * tickInterval, DamageType.Physical, ElementalType.Ice, tickTags);
            tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        target.tenacityMultiplier += tenacityReduction;
    }
}
