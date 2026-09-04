using UnityEngine;

// same shape/logic as BurnEffect (water weather quartering the duration, source.burnDurationBonus
// extension, particles/light) minus the parts that only ever make sense for a plant-sourced
// effect landing on an insect: no elemental affinity scaling and no Flammable amplification,
// since Scorch's source is always an insect and its target is always a plant. flat 12 Fire Magic
// damage per second, 8s duration, 1s tick interval
public class ScorchEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public float damagePerTick = 12f;
    private LightFader _scorchFader;
    private const float ScorchLightRadius = 1.25f;
    private ParticleSystem _scorchParticles;

    // the fully-resolved starting duration (after burnDurationBonus/weather above), captured
    // once so external "reduce Scorch by X% of its original duration" hooks (e.g. Aloe Vera's
    // hidden anti-Scorch passive/skill) always shave off a fixed amount, not a shrinking one
    public readonly float originalDuration;

    public ScorchEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
        tickInterval = 1f;
        // the source can extend the Scorches it causes, same hook Burn uses
        if (source != null) this.duration *= 1f + source.burnDurationBonus;
        // water douses fire: while the level is Underwater, Scorch only lasts a quarter as long
        if (WeatherManager.instance != null && WeatherManager.instance.HasWeather(WeatherType.Underwater))
            this.duration *= 0.25f;
        originalDuration = this.duration;
    }

    // shaves a fixed fraction of the original (fully-resolved) duration off however much of
    // this Scorch is left, e.g. Aloe Vera's heal reducing it by 33%/10% of its starting length
    public void ReduceByFraction(float fraction) => duration = Mathf.Max(0f, duration - originalDuration * fraction);

    public override string GetName() => "<color=#FF4500>Scorch</color>";
    public override string GetDescription() =>
        $"Deal <color=#FF4500><b>{damagePerTick:F0}</b></color> <color=#FF4500>Fire</color> <color=#FFB6C1>Magic</color> damage per second.";

    public override void OnApply()
    {
        base.OnApply();
        damagePerSecond = damagePerTick;

        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Scorch", new Color(1f, 0.27f, 0f));

        if (DarknessManager.instance != null)
        {
            _scorchFader = GetOrCreateScorchFader();
            _scorchFader.FadeIn(0.05f);
            DarknessManager.UnregisterLightSource(_scorchFader.transform);
            DarknessManager.RegisterLightSource(_scorchFader.transform, ScorchLightRadius);
        }

        SpawnScorchParticles();
    }

    private void SpawnScorchParticles()
    {
        Transform visualRoot = target.transform.Find("Visual") ?? target.transform;

        // reuse the existing system on a Scorch refresh so we never stack plumes
        Transform existing = visualRoot.Find("ScorchParticles");
        if (existing != null)
        {
            _scorchParticles = existing.GetComponent<ParticleSystem>();
            if (_scorchParticles != null) { _scorchParticles.Play(); return; }
        }

        // no dedicated Scorch particle art yet - reuses Burn's prefab as a placeholder
        GameObject prefab = Resources.Load<GameObject>("BurnParticles");
        if (prefab == null) return;

        GameObject fx = Object.Instantiate(prefab, visualRoot.position, Quaternion.identity);
        fx.name = "ScorchParticles";   // named so a refresh can find and reuse it
        fx.transform.SetParent(visualRoot);
        fx.transform.localPosition = Vector3.zero;
        // keep its own scale at 1 so the target's scale can't distort particle size or speed
        fx.transform.localScale = Vector3.one;
        _scorchParticles = fx.GetComponent<ParticleSystem>();
    }

    // scorch ended: stop emitting and let the embers fade. kept parented and idle so the
    // next Scorch reuses it instead of spawning a second system
    private void StopScorchParticles()
    {
        if (_scorchParticles == null) return;
        _scorchParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _scorchParticles = null;
    }

    // target died: unparent so the embers outlive it, then stop and clean up
    private void ReleaseScorchParticles()
    {
        if (_scorchParticles == null) return;
        _scorchParticles.transform.SetParent(null);
        // unparenting rewrites localScale to preserve world size; force it back to 1
        // so Local scaling mode keeps the embers at full size (no shrink pop)
        _scorchParticles.transform.localScale = Vector3.one;
        _scorchParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        float life = _scorchParticles.main.startLifetime.constantMax;
        Object.Destroy(_scorchParticles.gameObject, life);
        _scorchParticles = null;
    }

    private LightFader GetOrCreateScorchFader()
    {
        Transform visualRoot = target.transform.Find("Visual") ?? target.transform;

        Transform existing = visualRoot.Find("ScorchLight");
        if (existing != null)
        {
            var fader = existing.GetComponent<LightFader>();
            if (fader != null) return fader;
        }

        GameObject lightObj = new GameObject("ScorchLight");
        lightObj.transform.SetParent(visualRoot);
        lightObj.transform.localPosition = Vector3.zero;

        var light = lightObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = Color.white;
        light.falloffIntensity = 0.5f;
        light.pointLightOuterRadius = ScorchLightRadius;
        light.pointLightInnerRadius = ScorchLightRadius * 0.3f;

        var newFader = lightObj.AddComponent<LightFader>();
        newFader.Setup(light, 0.4f);
        return newFader;
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
            float tick = damagePerSecond * tickInterval;
            if (source != null)
                target.Damage(tick, DamageType.Magic, ElementalType.Fire, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
            else
                target.Damage(tick, DamageType.Magic, ElementalType.Fire, tickTags);
            tickTimer -= tickInterval;
        }
    }

    public override void OnExpire()
    {
        StopScorchParticles();

        if (_scorchFader != null)
        {
            DarknessManager.UnregisterLightSource(_scorchFader.transform);
            _scorchFader.FadeOut(2.5f);
        }
    }

    public override void OnTargetDied()
    {
        ReleaseScorchParticles();

        if (_scorchFader == null) return;
        DarknessManager.UnregisterLightSource(_scorchFader.transform);
        _scorchFader.transform.SetParent(null);
        _scorchFader.FadeOut(3f, destroyOnComplete: true);
        _scorchFader = null;
    }
}
