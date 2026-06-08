using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

// autonomous wisp summoned by the Gloriosa skill.
// one wisp per plant (claim dict covers both seeking and latched states).
// seeking: injured plants first (urgency override every frame), then plants with least latch time.
// latched: leaves after 1s or plant full health, looks for a more-injured plant next.
public class FieryWisp : MonoBehaviour
{
    private Gloriosa _source;
    private float _lifetime;
    private float _speed;
    private float _radius;
    private float _healPerSecond;
    private float _temperaturePerSecond;
    private float _latchHealPerSecond;
    private float _latchFireDamageFrac;
    private float _latchDuration;
    private float _seekDelay;
    private float _emergeSpeed;
    private float _tickInterval;

    private Vector3 _emergeVelocity;
    private float   _emergeTimer;
    private float   _timer;
    private float   _auraTimer;

    private Plant   _latchedPlant;
    private Plant   _claimedPlant;
    private bool    _isLatched;
    private float   _latchTimer;
    private float   _latchRefreshTimer;
    private const float LatchRefreshInterval = 0.4f;

    private Plant   _seekingTarget;
    private bool    _isRedirecting;
    private float   _redirectPauseTimer;
    private Vector3 _seekDirection;
    private const float RedirectPauseDuration = 0.5f;

    private Light2D _light;
    private bool _cleanedUp;
    private bool _isDying;

    private GameObject     _lifetimeBarInstance;
    private Transform      _lifetimeBarFill;
    private SpriteRenderer[] _allRenderers;
    private const float FadeDuration = 0.6f;

    // one wisp per plant: maps plant to the wisp that owns it (seeking or latched)
    private static readonly Dictionary<Plant, FieryWisp> _occupiedBy = new Dictionary<Plant, FieryWisp>();

    public void Initialize(Gloriosa source, float lifetime, float speed, float radius,
                           float healPerSecond, float temperaturePerSecond,
                           float latchHealPerSecond, float latchFireDamageFrac,
                           float latchDuration, float lightRadius, float lightIntensity, float emergeSpeed, float seekDelay, float tickInterval)
    {
        _source               = source;
        _lifetime             = lifetime;
        _speed                = speed;
        _radius               = radius;
        _healPerSecond        = healPerSecond;
        _temperaturePerSecond = temperaturePerSecond;
        _latchHealPerSecond   = latchHealPerSecond;
        _latchFireDamageFrac  = latchFireDamageFrac;
        _latchDuration        = latchDuration;
        _seekDelay    = seekDelay;
        _emergeSpeed  = emergeSpeed;
        _tickInterval = Mathf.Max(tickInterval, 0.05f);

        float angle     = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        _emergeVelocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _emergeSpeed;

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        _light = gameObject.AddComponent<Light2D>();
        _light.lightType             = Light2D.LightType.Point;
        _light.color                 = Color.white;
        _light.intensity             = lightIntensity;
        _light.pointLightOuterRadius = lightRadius;
        _light.pointLightInnerRadius = lightRadius * 0.3f;
        _light.falloffIntensity      = 0.5f;
        _light.enabled               = false;

        DarknessManager.RegisterLightSource(transform, lightRadius);

        GameObject barPrefab = Resources.Load<GameObject>("HealthBar");
        if (barPrefab != null)
        {
            _lifetimeBarInstance = Instantiate(barPrefab, transform);
            _lifetimeBarInstance.transform.localPosition = new Vector3(-0.35625f, 0.6f, 0f);
            Vector3 bs = _lifetimeBarInstance.transform.localScale;
            bs.y *= 1.6f;
            _lifetimeBarInstance.transform.localScale = bs;
            _lifetimeBarFill = _lifetimeBarInstance.transform.Find("Fill");
            if (_lifetimeBarFill != null)
            {
                SpriteRenderer fillSR = _lifetimeBarFill.GetComponent<SpriteRenderer>();
                if (fillSR != null) fillSR.color = new Color(0.65f, 0.65f, 0.65f);
                Vector3 fs = _lifetimeBarFill.localScale;
                fs.x = 1f;
                _lifetimeBarFill.localScale = fs;
            }
            _lifetimeBarInstance.SetActive(true);
        }
        _allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    void Update()
    {
        if (_isDying) return;

        _timer += Time.deltaTime;

        if (_lifetimeBarFill != null)
        {
            Vector3 s = _lifetimeBarFill.localScale;
            s.x = Mathf.Clamp01(1f - _timer / _lifetime);
            _lifetimeBarFill.localScale = s;
        }

        if (_timer >= _lifetime)
        {
            _isDying = true;
            Cleanup();
            StartCoroutine(FadeAndDie());
            return;
        }

        if (_light != null)
            _light.enabled = DarknessManager.instance != null && DarknessManager.instance.isDark;

        _emergeTimer += Time.deltaTime;
        if (_emergeTimer < _seekDelay)
        {
            float t = _seekDelay > 0f ? _emergeTimer / _seekDelay : 1f;
            transform.position += _emergeVelocity * (1f - t) * Time.deltaTime;
            return;
        }

        if (_isLatched) UpdateLatched();
        else            UpdateSeeking();

        UpdateAura();
    }

    private void UpdateSeeking()
    {
        Plant target = FindTarget();
        if (target == null) { ReleaseClaim(); return; }

        // re-claim every frame to support urgency override (injured plant changes target instantly)
        ClaimPlant(target);

        if (target != _seekingTarget)
        {
            if (_seekingTarget != null)
            {
                // store the direction we were moving so we can decelerate along it
                _seekDirection      = (_seekingTarget.transform.position - transform.position).normalized;
                _isRedirecting      = true;
                _redirectPauseTimer = 0f;
            }
            _seekingTarget = target;
        }

        if (_isRedirecting)
        {
            _redirectPauseTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_redirectPauseTimer / RedirectPauseDuration);
            transform.position  += _seekDirection * _speed * (1f - t) * Time.deltaTime;
            if (_redirectPauseTimer >= RedirectPauseDuration)
                _isRedirecting = false;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.transform.position, _speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.transform.position) < 0.3f)
            Latch(target);
    }

    private void UpdateLatched()
    {
        if (_latchedPlant == null || !_latchedPlant.IsAlive) { Unlatch(); return; }

        transform.position = _latchedPlant.transform.position;
        _latchTimer        += Time.deltaTime;
        _latchRefreshTimer -= Time.deltaTime;

        if (_latchRefreshTimer <= 0f)
        {
            _latchRefreshTimer = LatchRefreshInterval;
            _latchedPlant.ApplyEffect(new FieryWispLatchedEffect(
                _latchedPlant, _latchDuration, 1, _source, _latchHealPerSecond, _latchFireDamageFrac, _tickInterval));
        }

        if (_latchTimer < 1f) return;

        bool plantFull = _latchedPlant.health >= _latchedPlant.maxHealth;

        // time to leave: only unlatch if there is somewhere better to go
        float thresholdFrac = plantFull ? 1f : _latchedPlant.health / _latchedPlant.maxHealth;
        if (FindNextTarget(thresholdFrac) != null)
            Unlatch();
    }

    // seeking: priority 1 = most injured unoccupied plant; priority 2 = unoccupied plant with least latch time
    private Plant FindTarget()
    {
        Plant best     = null;
        float bestFrac = 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || IsOccupied(plant)) continue;
            if (plant.health >= plant.maxHealth) continue;
            float frac = plant.health / plant.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; best = plant; }
        }
        if (best != null) return best;

        // fallback: unoccupied plant with least remaining FieryWispLatchedEffect time (0 = no effect)
        float leastTime = float.MaxValue;
        float leastFrac = 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || IsOccupied(plant)) continue;
            float remaining = GetLatchRemainingTime(plant);
            float frac      = plant.maxHealth > 0f ? plant.health / plant.maxHealth : 1f;
            if (remaining < leastTime || (remaining == leastTime && frac < leastFrac))
            {
                leastTime = remaining;
                leastFrac = frac;
                best = plant;
            }
        }
        return best;
    }

    // post-latch: injured plant with lower frac than threshold, else least latch time
    private Plant FindNextTarget(float thresholdFrac)
    {
        Plant best     = null;
        float bestFrac = thresholdFrac; // must be strictly less than threshold
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == _latchedPlant || IsOccupied(plant)) continue;
            if (plant.health >= plant.maxHealth) continue;
            float frac = plant.health / plant.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; best = plant; }
        }
        if (best != null) return best;

        // fallback: unoccupied plant (excluding self) with least remaining latch time
        float leastTime = float.MaxValue;
        float leastFrac = 1f;
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == _latchedPlant || IsOccupied(plant)) continue;
            float remaining = GetLatchRemainingTime(plant);
            float frac      = plant.maxHealth > 0f ? plant.health / plant.maxHealth : 1f;
            if (remaining < leastTime || (remaining == leastTime && frac < leastFrac))
            {
                leastTime = remaining;
                leastFrac = frac;
                best = plant;
            }
        }
        return best;
    }

    private float GetLatchRemainingTime(Plant plant)
    {
        foreach (StatusEffect e in plant.activeEffects)
            if (e is FieryWispLatchedEffect) return e.duration;
        return 0f;
    }

    private bool IsOccupied(Plant plant) =>
        _occupiedBy.TryGetValue(plant, out var wisp) && wisp != this;

    private void ClaimPlant(Plant plant)
    {
        if (_claimedPlant == plant) return;
        ReleaseClaim();
        _claimedPlant = plant;
        _occupiedBy[plant] = this;
    }

    private void ReleaseClaim()
    {
        if (_claimedPlant == null) return;
        if (_occupiedBy.TryGetValue(_claimedPlant, out var wisp) && wisp == this)
            _occupiedBy.Remove(_claimedPlant);
        _claimedPlant = null;
    }

    private void Latch(Plant plant)
    {
        ClaimPlant(plant);
        _latchedPlant      = plant;
        _isLatched         = true;
        _latchTimer        = 0f;
        _latchRefreshTimer = LatchRefreshInterval;
        plant.ApplyEffect(new FieryWispLatchedEffect(
            plant, _latchDuration, 1, _source, _latchHealPerSecond, _latchFireDamageFrac, _tickInterval));
    }

    private void Unlatch()
    {
        ReleaseClaim();
        _latchedPlant       = null;
        _isLatched          = false;
        _latchTimer         = 0f;
        _latchRefreshTimer  = 0f;
        _seekingTarget      = null;
        _isRedirecting      = false;
        _redirectPauseTimer = 0f;

        // re-trigger emerge so the wisp drifts outward before seeking the next plant
        _emergeTimer    = 0f;
        float angle     = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        _emergeVelocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _emergeSpeed;
    }

    private void UpdateAura()
    {
        _auraTimer += Time.deltaTime;
        if (_auraTimer < _tickInterval) return;
        _auraTimer -= _tickInterval;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > _radius) continue;
            plant.Heal(_healPerSecond * _tickInterval, _source);
            if (WeatherManager.instance?.temperature == TemperatureType.Cold)
                plant.temperature = Mathf.Min(plant.temperature + _temperaturePerSecond * _tickInterval, 10f);
        }
    }

    private IEnumerator FadeAndDie()
    {
        float startIntensity = _light != null ? _light.intensity : 0f;
        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - elapsed / FadeDuration);
            if (_allRenderers != null)
                foreach (SpriteRenderer sr in _allRenderers)
                    if (sr != null) { Color c = sr.color; c.a = alpha; sr.color = c; }
            if (_light != null) _light.intensity = startIntensity * alpha;
            yield return null;
        }
        Destroy(gameObject);
    }

    public void Despawn()
    {
        Cleanup();
        Destroy(gameObject);
    }

    private void Cleanup()
    {
        if (_cleanedUp) return;
        _cleanedUp = true;
        if (_isLatched) Unlatch();
        else            ReleaseClaim();
        _source?.UnregisterWisp(this);
        DarknessManager.UnregisterLightSource(transform);
    }

    void OnDestroy() => Cleanup();
}
