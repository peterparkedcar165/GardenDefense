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

    void Update()
    {
        // retarget if current target is gone
        if (_target == null || !_target.IsAlive)
        {
            _target = _source != null ? _source.FindCurrentTarget() : null;
            if (_target == null) { Destroy(gameObject); return; }
        }

        Vector3 targetPos = ((Entity)_target).transform.position;
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

        if (_target is Plant hitPlant)
        {
            hitPlant.Heal(_healAmount, _source);
            hitPlant.ApplyEffect(new WarmingEffect(hitPlant, 2f, 1, _source, _temperatureAmount));

            foreach (Plant nearby in new List<Plant>(Plant.allPlants))
            {
                if (nearby == null || !nearby.IsAlive || nearby == hitPlant) continue;
                if (Vector3.Distance(transform.position, nearby.transform.position) > _auraRadius) continue;
                nearby.Heal(_healAmount * 0.5f, _source);
                nearby.ApplyEffect(new WarmingEffect(nearby, 2f, 1, _source, _temperatureAmount * 0.5f));
            }
        }
        else if (_target is Insect insect)
        {
            insect.Damage(_damage, _damageType, _elementalType, _source, true, _damageTags);
        }
    }
}
