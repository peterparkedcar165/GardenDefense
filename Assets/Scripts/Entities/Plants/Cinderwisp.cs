using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

// autonomous wisp summoned by the Gloriosa skill.
// one wisp per plant (claim dict covers both seeking and latched states).
// seeking: injured plants first (urgency override every frame), then plants with least latch time.
// latched: leaves after 1s or plant full health, looks for a more-injured plant next.
public class Cinderwisp : MonoBehaviour
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

    // once every plant already has Boon of The Wisp (any duration) and it's dark, wisps stop
    // topping off a fully-covered garden and instead go reveal the deepest hidden threat
    private bool    _isChasingDarkness;
    private Insect  _chasedInsect;

    private Light2D _light;
    private bool _cleanedUp;
    private bool _isDying;

    private GameObject     _lifetimeBarInstance;
    private Transform      _lifetimeBarFill;
    private SpriteRenderer[] _allRenderers;
    private const float FadeDuration = 0.6f;

    // one wisp per (plant, source gloriosa): maps a plant+gloriosa pair to the wisp that owns
    // it (seeking or latched), so a different gloriosa's wisps aren't blocked by this claim
    private static readonly Dictionary<(Plant, Gloriosa), Cinderwisp> _occupiedBy = new Dictionary<(Plant, Gloriosa), Cinderwisp>();

    public void Initialize(Gloriosa source, float lifetime, float speed, float radius,
                           float healPerSecond, float temperaturePerSecond,
                           float latchHealPerSecond, float latchFireDamageFrac,
                           float latchDuration, float lightIntensity, float emergeSpeed, float seekDelay, float tickInterval)
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
        _light.pointLightOuterRadius = radius;
        _light.pointLightInnerRadius = radius * 0.3f;
        _light.falloffIntensity      = 0.5f;
        _light.enabled               = false;

        // illumination range matches the healing aura radius, so a wisp lights up exactly the
        // area it's also healing/warming
        DarknessManager.RegisterLightSource(transform, radius);

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

        if (_isChasingDarkness)
        {
            UpdateDarknessChase();
        }
        else if (!HasUnoccupiedInjuredPlant() && ShouldChaseDarkness())
        {
            if (_isLatched) Unlatch();
            _isChasingDarkness = true;
            UpdateDarknessChase();
        }
        else
        {
            if (_isLatched) UpdateLatched();
            else            UpdateSeeking();
        }

        UpdateAura();
    }

    // lowest priority tier: only while it's dark, and only once nothing with higher priority
    // (an injured plant, or a plant with no Boon of The Wisp at all) is waiting for a wisp.
    // "has Boon of The Wisp" only cares about presence, not remaining duration
    private bool ShouldChaseDarkness()
    {
        if (DarknessManager.instance == null || !DarknessManager.instance.isDark) return false;
        return !HasUnoccupiedPlantWithoutBoonOfTheWisp();
    }

    // priority 1: an unoccupied plant that needs healing
    private bool HasUnoccupiedInjuredPlant()
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || IsOccupied(plant)) continue;
            if (plant.health < plant.maxHealth) return true;
        }
        return false;
    }

    // priority 2: an unoccupied plant with no Boon of The Wisp at all, regardless of health
    private bool HasUnoccupiedPlantWithoutBoonOfTheWisp()
    {
        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || IsOccupied(plant)) continue;
            if (!plant.HasEffect<BoonOfTheWispEffect>()) return true;
        }
        return false;
    }

    // priority 3: holds the same insect until it dies (re-target) or a higher-priority plant
    // need appears (drop the chase entirely and let normal seeking pick it up)
    private void UpdateDarknessChase()
    {
        if (HasUnoccupiedInjuredPlant() || HasUnoccupiedPlantWithoutBoonOfTheWisp())
        {
            StopChasingDarkness();
            return;
        }

        if (_chasedInsect == null || !_chasedInsect.IsAlive)
        {
            ReleaseInsectClaim();
            _chasedInsect = FindDarknessTarget();
            if (_chasedInsect != null) ClaimInsect(_chasedInsect);
        }

        if (_chasedInsect == null)
        {
            StopChasingDarkness();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, _chasedInsect.GetAimPoint(), _speed * Time.deltaTime);
    }

    private void StopChasingDarkness()
    {
        _isChasingDarkness = false;
        ReleaseInsectClaim();
        _chasedInsect = null;
    }

    // the non-illuminated, unclaimed insect furthest along its path: lowest waypoint index first,
    // tied broken by whichever is furthest from its next waypoint (mirrors Plant's Last targeting)
    private Insect FindDarknessTarget()
    {
        Insect best = null;
        int lowestWaypointIndex = int.MaxValue;
        float furthestDistToNext = -1f;
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || !IsHiddenInDarkness(insect) || IsInsectOccupied(insect)) continue;
            Transform waypoint = insect.GetCurrentWaypoint();
            if (waypoint == null) continue;
            if (insect.currentWaypointIndex < lowestWaypointIndex)
            {
                lowestWaypointIndex = insect.currentWaypointIndex;
                furthestDistToNext = Vector3.Distance(insect.transform.position, waypoint.position);
                best = insect;
            }
            else if (insect.currentWaypointIndex == lowestWaypointIndex)
            {
                float d = Vector3.Distance(insect.transform.position, waypoint.position);
                if (d > furthestDistToNext) { furthestDistToNext = d; best = insect; }
            }
        }
        return best;
    }

    private static bool IsHiddenInDarkness(Insect insect) =>
        DarknessManager.instance != null && DarknessManager.instance.isDark &&
        !DarknessManager.instance.IsIlluminated(insect.transform.position);

    // one wisp per insect, mirrors the plant-occupation dictionary above
    private static readonly Dictionary<Insect, Cinderwisp> _insectOccupiedBy = new Dictionary<Insect, Cinderwisp>();

    private bool IsInsectOccupied(Insect insect) =>
        _insectOccupiedBy.TryGetValue(insect, out var wisp) && wisp != this;

    private void ClaimInsect(Insect insect) => _insectOccupiedBy[insect] = this;

    private void ReleaseInsectClaim()
    {
        if (_chasedInsect == null) return;
        if (_insectOccupiedBy.TryGetValue(_chasedInsect, out var wisp) && wisp == this)
            _insectOccupiedBy.Remove(_chasedInsect);
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
            _latchedPlant.ApplyEffect(new BoonOfTheWispEffect(
                _latchedPlant, _latchDuration, 1, _source, _latchHealPerSecond, _latchFireDamageFrac, _tickInterval));
        }

        if (_latchTimer < 1f) return;

        bool plantFull = _latchedPlant.health >= _latchedPlant.maxHealth;

        // time to leave: only unlatch if there is somewhere better to go
        float thresholdFrac = plantFull ? 1f : _latchedPlant.health / _latchedPlant.maxHealth;
        if (FindNextTarget(thresholdFrac) != null)
            Unlatch();
    }

    // seeking: priority 1 = most injured unoccupied plant; priority 2 = unoccupied plant with no
    // Boon of The Wisp at all yet (presence only, not remaining duration)
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

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || IsOccupied(plant)) continue;
            if (!plant.HasEffect<BoonOfTheWispEffect>()) return plant;
        }
        return null;
    }

    // post-latch: injured plant with lower frac than threshold, else any unoccupied plant with
    // no Boon of The Wisp at all yet
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

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == _latchedPlant || IsOccupied(plant)) continue;
            if (!plant.HasEffect<BoonOfTheWispEffect>()) return plant;
        }
        return null;
    }

    // occupation is scoped per source gloriosa: a plant claimed by one gloriosa's wisp is still
    // free for another gloriosa's wisp to also latch onto (each contributes its own stackable
    // Boon of The Wisp instance)
    private bool IsOccupied(Plant plant) =>
        _occupiedBy.TryGetValue((plant, _source), out var wisp) && wisp != this;

    private void ClaimPlant(Plant plant)
    {
        if (_claimedPlant == plant) return;
        ReleaseClaim();
        _claimedPlant = plant;
        _occupiedBy[(plant, _source)] = this;
    }

    private void ReleaseClaim()
    {
        if (_claimedPlant == null) return;
        if (_occupiedBy.TryGetValue((_claimedPlant, _source), out var wisp) && wisp == this)
            _occupiedBy.Remove((_claimedPlant, _source));
        _claimedPlant = null;
    }

    private void Latch(Plant plant)
    {
        ClaimPlant(plant);
        _latchedPlant      = plant;
        _isLatched         = true;
        _latchTimer        = 0f;
        _latchRefreshTimer = LatchRefreshInterval;
        plant.ApplyEffect(new BoonOfTheWispEffect(
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
        ReleaseInsectClaim();
        _source?.UnregisterWisp(this);
        DarknessManager.UnregisterLightSource(transform);
    }

    void OnDestroy() => Cleanup();
}
