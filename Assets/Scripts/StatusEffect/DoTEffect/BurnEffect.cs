using UnityEngine;

public class BurnEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public float healthPerSecond = 0.05f, mpPerSecond = 0.36f;
    private float cachedMaxHealth;
    private float cachedMagicPower;
    private LightFader _burnFader;
    private const float BurnLightRadius = 1.25f;

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
        float total = (healthPerSecond * hp) + (mpPerSecond * mp) + 6f;
        return $"Deal <color=orange><b>{total:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage per second. (<color=red>{healthPerSecond * 100:F0}% Max Health</color> + <color=#FFB6C1>{mpPerSecond * 100:F0}% Magic Power</color> + 6)";
    }

    public override void OnApply()
    {
        base.OnApply();
        cachedMaxHealth = target.maxHealth;
        cachedMagicPower = source?.magicPower ?? 0f;
        damagePerSecond = (healthPerSecond * cachedMaxHealth) + (mpPerSecond * cachedMagicPower) + 6f;
        Debug.Log($"Burn applied by {source} to {target}");

        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Burn", new Color(1f, 0.4f, 0f));

        if (DarknessManager.instance != null)
        {
            _burnFader = GetOrCreateBurnFader();
            _burnFader.FadeIn(0.05f);
            DarknessManager.UnregisterLightSource(_burnFader.transform);
            DarknessManager.RegisterLightSource(_burnFader.transform, BurnLightRadius);
        }
    }

    private LightFader GetOrCreateBurnFader()
    {
        Transform visualRoot = target.transform.Find("Visual") ?? target.transform;

        Transform existing = visualRoot.Find("BurnLight");
        if (existing != null)
        {
            var fader = existing.GetComponent<LightFader>();
            if (fader != null) return fader;
        }

        GameObject lightObj = new GameObject("BurnLight");
        lightObj.transform.SetParent(visualRoot);
        lightObj.transform.localPosition = Vector3.zero;

        var light = lightObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = Color.white;
        light.falloffIntensity = 0.5f;
        light.pointLightOuterRadius = BurnLightRadius;
        light.pointLightInnerRadius = BurnLightRadius * 0.3f;

        var newFader = lightObj.AddComponent<LightFader>();
        newFader.Setup(light, 0.4f);
        return newFader;
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
            if (source != null)
                target.Damage(damagePerSecond * tickInterval, DamageType.Magic, ElementalType.Fire, source, false, tickTags);
            else
                target.Damage(damagePerSecond * tickInterval, DamageType.Magic, ElementalType.Fire, tickTags);
            tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        if (_burnFader != null)
        {
            DarknessManager.UnregisterLightSource(_burnFader.transform);
            _burnFader.FadeOut(2.5f);
        }
        Debug.Log("Burn expired");
    }

    public override void OnTargetDied()
    {
        if (_burnFader == null) return;
        DarknessManager.UnregisterLightSource(_burnFader.transform);
        _burnFader.transform.SetParent(null);
        _burnFader.FadeOut(3f, destroyOnComplete: true);
        _burnFader = null;
    }
}
