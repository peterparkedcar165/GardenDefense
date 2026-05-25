using UnityEngine;

public class FrostbiteEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public float healthPerSecond = 0.04f, adPerSecond = 0.06f;
    private float tenacityReduction = 0.66f;
    private float cachedElementalPower;

    public FrostbiteEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        cachedElementalPower = source?.elementalPower ?? 0f;
        effectType = Type.negative;
        tickInterval = 0.25f;
    }

    public override string GetName() => "<color=#00BFFF>Frostbite</color>";
    public override string GetDescription()
    {
        float ep = cachedElementalPower;
        return $"Deal (<color=green>{healthPerSecond * 100}%</color> Max Health + <color=green>{adPerSecond * 100}%</color> Attack Damage) × (1 + <color=#FFD700>{ep * 100:F0}% Elemental Power</color>) <color=#00BFFF>Ice</color> Physical damage per second. Reduces Tenacity by <color=green>{tenacityReduction * 100}%</color>.";
    }

    public override void OnApply()
    {
        base.OnApply();
        damagePerSecond = ((healthPerSecond * target.maxHealth) + (adPerSecond * source.attackDamage) + 2f) * (1f + cachedElementalPower);

        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Frostbite", new Color(0f, 1f, 1f));

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
