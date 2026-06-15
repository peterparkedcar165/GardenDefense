using UnityEngine;
using System.Collections.Generic;

// homing ember fired by the Gloriosa.
// has no collider and does not detonate on contact with other plants.
// only detonates when it physically reaches its tracked target.
// if the target dies in flight the projectile asks the Gloriosa for a new target and retracks.
public class EmberProjectile : MonoBehaviour
{
    private Gloriosa     _source;
    private IAttackable   _target;
    private float         _speed;
    private float         _healAmount;
    private float         _temperatureAmount;
    private float         _auraRadius;
    private float         _damage;
    private DamageType    _damageType;
    private ElementalType _elementalType;
    private bool          _isBounce;
    private int           _bouncesRemaining;

    private static readonly DamageTag[] _damageTags = { DamageTag.Attack, DamageTag.Projectile };

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    public void Initialize(Gloriosa source, IAttackable target,
                           float speed, float healAmount, float temperatureAmount,
                           float auraRadius, float damage,
                           DamageType damageType, ElementalType elementalType)
    {
        _source            = source;
        _target            = target;
        _speed             = speed;
        _healAmount        = healAmount;
        _temperatureAmount = temperatureAmount;
        _auraRadius        = auraRadius;
        _damage            = damage;
        _damageType        = damageType;
        _elementalType     = elementalType;

        if (transform.childCount > 0)
            transform.GetChild(0).GetComponent<SpriteRenderer>()?.gameObject.SetActive(true);
    }

    public void SetBouncesRemaining(int count) => _bouncesRemaining = count;
    public void MarkAsBounce()                 => _isBounce = true;

    void Update()
    {
        if (_target == null || !_target.IsAlive)
        {
            // bounce embers only ever seek friendly units
            _target = _isBounce ? FindBounceTarget(null) : (_source != null ? _source.FindCurrentTarget() : null);
            if (_target == null) { Destroy(gameObject); return; }
        }

        Insect targetInsect = _target as Insect;
        Vector3 targetPos = targetInsect != null ? targetInsect.GetAimPoint() : ((Entity)_target).transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, _speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) < 0.15f)
        {
            Detonate();
            Destroy(gameObject);
        }
    }

    private void Detonate()
    {
        if (_target == null || !_target.IsAlive) return;

        bool targetWasFriendly = false;

        Plant primaryPlant = _target as Plant;
        if (primaryPlant != null)
        {
            targetWasFriendly = true;
            primaryPlant.Heal(_healAmount, _source);
            if (WeatherManager.instance?.temperature == TemperatureType.Cold)
                primaryPlant.temperature = Mathf.Min(primaryPlant.temperature + _temperatureAmount, primaryPlant.comfortMax);
            if (_source != null && _source.IsPath2Maxed)
                primaryPlant.ApplyEffect(new HeatedComfortEffect(primaryPlant, _healAmount, _temperatureAmount, _source));
        }
        else if (_target is Insect insect)
        {
            if (insect.team == Team.Friendly)
            {
                targetWasFriendly = true;
                insect.Heal(_healAmount, _source);
            }
            else
            {
                insect.Damage(_damage, _damageType, _elementalType, _source, true, _damageTags);
            }
        }

        // aoe splash: heals and warms nearby plants regardless of primary target
        foreach (Plant nearby in new List<Plant>(Plant.allPlants))
        {
            if (nearby == null || !nearby.IsAlive || nearby == primaryPlant) continue;
            if (Vector3.Distance(transform.position, nearby.transform.position) > _auraRadius) continue;
            nearby.Heal(_healAmount * 0.5f, _source);
            if (WeatherManager.instance?.temperature == TemperatureType.Cold)
                nearby.temperature = Mathf.Min(nearby.temperature + _temperatureAmount * 0.5f, nearby.comfortMax);
        }

        // path 1 max level: bounce to another friendly unit, chain limited by bouncesRemaining
        if (targetWasFriendly && _source != null && _source.IsPath1Maxed && _bouncesRemaining > 0)
        {
            IAttackable bounceTarget = FindBounceTarget(_target);
            if (bounceTarget != null)
                _source.SpawnBounceEmber(transform.position, bounceTarget, _bouncesRemaining - 1);
        }
    }

    // lowest health fraction first; if all are full, coldest plant instead. never targets enemies.
    // search is limited to the Gloriosa's attack range from the bounce origin (current position).
    private IAttackable FindBounceTarget(IAttackable justHit)
    {
        float range = _source != null ? _source.attackRange : float.MaxValue;

        IAttackable best = null;
        float bestFrac = float.MaxValue;

        foreach (Plant plant in Plant.allPlants)
        {
            if (plant == null || !plant.IsAlive || plant == justHit) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) > range) continue;
            float frac = plant.health / plant.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; best = plant; }
        }

        foreach (Insect ally in Insect.friendlyInsects)
        {
            if (ally == null || !ally.IsAlive || ally == justHit) continue;
            if (Vector3.Distance(transform.position, ((Entity)ally).transform.position) > range) continue;
            float frac = ally.health / ally.maxHealth;
            if (frac < bestFrac) { bestFrac = frac; best = ally; }
        }

        // if all are at full health, fall back to the coldest plant in range
        if (bestFrac >= 1f)
        {
            Plant coldest = null;
            float lowestTemp = float.MaxValue;
            foreach (Plant plant in Plant.allPlants)
            {
                if (plant == null || !plant.IsAlive || plant == justHit) continue;
                if (Vector3.Distance(transform.position, plant.transform.position) > range) continue;
                if (plant.temperature < lowestTemp) { lowestTemp = plant.temperature; coldest = plant; }
            }
            best = coldest;
        }

        return best;
    }
}
