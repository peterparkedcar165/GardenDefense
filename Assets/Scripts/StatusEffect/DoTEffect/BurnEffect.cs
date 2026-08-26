using UnityEngine;

public class BurnEffect : DoTEffect, IElementalAffinityEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public float healthPerSecond = 0.01f, flatPerSecond = 18f;
    private float cachedMaxHealth;
    private float cachedelementalAffinity;
    private LightFader _burnFader;
    private const float BurnLightRadius = 1.25f;
    private ParticleSystem _burnParticles;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    public BurnEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        cachedelementalAffinity = source?.elementalAffinity ?? 0f;
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
        tickInterval = 1f;
        // the source can extend the Burns it causes (Stargazer passive)
        if (source != null) this.duration *= 1f + source.burnDurationBonus;
    }

    public override string GetName() => "<color=orange>Burn</color>";
    public override string GetDescription()
    {
        float hp = cachedMaxHealth > 0 ? cachedMaxHealth : (target?.maxHealth ?? 0f);
        float ep = cachedelementalAffinity;
        float flammable = target?.GetEffect<FlammableEffect>()?.BurnMultiplier ?? 1f;
        float total = (healthPerSecond * hp + flatPerSecond) * (1f + 0.33f * ep) * flammable;
        return $"Deal <color=orange><b>{total:F0}</b></color> <color=orange>Fire</color> <color=#FFB6C1>Magic</color> damage per second.";
    }

    public override void OnApply()
    {
        base.OnApply();
        cachedMaxHealth = target.maxHealth;
        damagePerSecond = (healthPerSecond * cachedMaxHealth + flatPerSecond) * (1f + 0.33f * cachedelementalAffinity);

        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Burn", new Color(1f, 0.4f, 0f));

        if (DarknessManager.instance != null)
        {
            _burnFader = GetOrCreateBurnFader();
            _burnFader.FadeIn(0.05f);
            DarknessManager.UnregisterLightSource(_burnFader.transform);
            DarknessManager.RegisterLightSource(_burnFader.transform, BurnLightRadius);
        }

        SpawnBurnParticles();
    }

    private void SpawnBurnParticles()
    {
        Transform visualRoot = target.transform.Find("Visual") ?? target.transform;

        // reuse the existing system on a Burn refresh so we never stack plumes
        Transform existing = visualRoot.Find("BurnParticles");
        if (existing != null)
        {
            _burnParticles = existing.GetComponent<ParticleSystem>();
            if (_burnParticles != null) { _burnParticles.Play(); return; }
        }

        GameObject prefab = Resources.Load<GameObject>("BurnParticles");
        if (prefab == null) return;

        GameObject fx = Object.Instantiate(prefab, visualRoot.position, Quaternion.identity);
        fx.name = "BurnParticles";   // named so a refresh can find and reuse it
        fx.transform.SetParent(visualRoot);
        fx.transform.localPosition = Vector3.zero;
        // keep its own scale at 1 so the insect's scale can't distort particle size or speed
        fx.transform.localScale = Vector3.one;
        _burnParticles = fx.GetComponent<ParticleSystem>();
    }

    // burn ended: stop emitting and let the embers fade. kept parented and idle so the
    // next Burn reuses it instead of spawning a second system
    private void StopBurnParticles()
    {
        if (_burnParticles == null) return;
        _burnParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _burnParticles = null;
    }

    // target died: unparent so the embers outlive it, then stop and clean up
    private void ReleaseBurnParticles()
    {
        if (_burnParticles == null) return;
        _burnParticles.transform.SetParent(null);
        // unparenting rewrites localScale to preserve world size; force it back to 1
        // so Local scaling mode keeps the embers at full size (no shrink pop)
        _burnParticles.transform.localScale = Vector3.one;
        _burnParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        float life = _burnParticles.main.startLifetime.constantMax;
        Object.Destroy(_burnParticles.gameObject, life);
        _burnParticles = null;
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
            // Flammable (from the Stargazer) amplifies Burn damage per stack
            float flammable = target.GetEffect<FlammableEffect>()?.BurnMultiplier ?? 1f;
            float tick = damagePerSecond * tickInterval * flammable;
            if (source != null)
                target.Damage(tick, DamageType.Magic, ElementalType.Fire, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
            else
                target.Damage(tick, DamageType.Magic, ElementalType.Fire, tickTags);
            tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        StopBurnParticles();

        if (_burnFader != null)
        {
            DarknessManager.UnregisterLightSource(_burnFader.transform);
            _burnFader.FadeOut(2.5f);
        }
    }

    public override void OnTargetDied()
    {
        ReleaseBurnParticles();

        if (_burnFader == null) return;
        DarknessManager.UnregisterLightSource(_burnFader.transform);
        _burnFader.transform.SetParent(null);
        _burnFader.FadeOut(3f, destroyOnComplete: true);
        _burnFader = null;
    }
}
