using UnityEngine;

public class BurnEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public float healthPerSecond = 0.03f, mpPerSecond = 0.36f;
    private float cachedMaxHealth;
    private float cachedMagicPower;

    public BurnEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        tickInterval = 0.5f;
    }

    public override string GetName() => "<color=orange>Burn</color>";
    public override string GetDescription()
    {
        float hp = cachedMaxHealth > 0 ? cachedMaxHealth : (target?.maxHealth ?? 0f);
        float mp = cachedMagicPower > 0 ? cachedMagicPower : (source?.magicPower ?? 0f);
        float total = (healthPerSecond * hp) + (mpPerSecond * mp) + 4f;
        return $"Deal <color=orange><b>{total:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage per second. (<color=red>{healthPerSecond * 100:F0}% Max Health</color> + <color=#FFB6C1>{mpPerSecond * 100:F0}% Magic Power</color> + 4)";
    }

    public override void OnApply()
    {
        base.OnApply();
        cachedMaxHealth = target.maxHealth;
        cachedMagicPower = source?.magicPower ?? 0f;
        damagePerSecond = (healthPerSecond * cachedMaxHealth) + (mpPerSecond * cachedMagicPower) + 4f;
        Debug.Log($"Burn applied by {source} to {target}");

        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Burn", new Color(1f, 0.4f, 0f));
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
            if (source != null)
                target.Damage((damagePerSecond * tickInterval), DamageType.Magic, ElementalType.Fire, source, false, tickTags);
            else
                target.Damage((damagePerSecond * tickInterval), DamageType.Magic, ElementalType.Fire, tickTags);
            tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        Debug.Log("Burn expired");
    }
}
