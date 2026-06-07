using UnityEngine;
using UnityEngine.Rendering.Universal;

// slow cave healer. stops in place to cast a single-target heal at the most injured insect nearby.
// set aggressivity to Low in InsectData so it never attacks plants
public class Glowworm : Insect
{
    private GlowwormData GData => data as GlowwormData;

    private float   _healTimer;
    private float   _castTimer;
    private bool    _isCasting;
    private Insect  _currentTarget;
    private Light2D _light;

    protected override void Awake()
    {
        base.Awake();
        LoadData();
        _healTimer = GData?.healCooldown ?? 3f;
    }

    public override void UpdateStats()
    {
        baseLightEmissionRange = GData?.glowRadius ?? 2f;
        base.UpdateStats();

        if (_light == null && visual != null)
        {
            _light = visual.gameObject.AddComponent<Light2D>();
            _light.lightType        = Light2D.LightType.Point;
            _light.color            = RegenerativeGlowEffect.GlowColor;
            _light.falloffIntensity = 0.5f;
        }

        if (_light != null)
        {
            _light.pointLightOuterRadius = GData?.glowRadius    ?? 2f;
            _light.pointLightInnerRadius = (GData?.glowRadius   ?? 2f) * 0.3f;
            _light.intensity             = GData?.glowIntensity ?? 0.8f;
        }
    }

    protected override void Move()
    {
        if (_isCasting) return;
        base.Move();
    }

    protected override void Update()
    {
        base.Update();

        if (_isCasting)
        {
            _castTimer -= Time.deltaTime;
            if (_castTimer <= 0f)
                FinishCast();
        }
        else
        {
            _healTimer -= Time.deltaTime;
            if (_healTimer <= 0f)
            {
                _currentTarget = FindMostInjuredInRange();
                if (_currentTarget != null)
                    BeginCast();
                else
                    _healTimer = GData?.healCooldown ?? 3f;
            }
        }
    }

    // returns the injured insect with the lowest health percentage within heal range, or null
    private Insect FindMostInjuredInRange()
    {
        float  range    = GData?.healRange ?? 4f;
        Insect best     = null;
        float  bestFrac = 1f;   // only consider < 1 (injured)

        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > range) continue;
            float frac = insect.health / insect.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; best = insect; }
        }
        return best;
    }

    private void BeginCast()
    {
        _isCasting = true;
        _castTimer = GData?.castDuration ?? 1.5f;
    }

    private void FinishCast()
    {
        _isCasting = false;
        _healTimer = GData?.healCooldown ?? 3f;

        if (!IsAlive || _currentTarget == null || !_currentTarget.IsAlive)
        {
            _currentTarget = null;
            return;
        }

        float flat        = GData?.healAmount            ?? 20f;
        float pct         = GData?.healPercent           ?? 0.1f;
        float regenDur    = GData?.regenerationDuration  ?? 6f;
        float regenHeal   = GData?.regenerationHealing   ?? 8f;
        float regenPct    = GData?.regenerationPercent   ?? 0.01f;
        float glowRadius  = GData?.glowRadius            ?? 2f;
        float glowIntens  = GData?.glowIntensity         ?? 0.8f;

        _currentTarget.Heal(flat + _currentTarget.maxHealth * pct, this);
        _currentTarget.ApplyEffect(new RegenerativeGlowEffect(
            _currentTarget, regenDur, 1, this, regenHeal, regenPct, glowRadius, glowIntens));

        _currentTarget = null;
    }
}
