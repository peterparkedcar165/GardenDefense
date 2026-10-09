using UnityEngine;
using System.Collections.Generic;

// a single, slow-moving pollen-seed projectile with a huge hitbox (Dandelion's skill). flies
// in a straight line: insects it touches take damage once and get swept along with the wind
// (WindDisplacedEffect), while allied plants it touches gain Pollen Haste once - never hitting
// the same entity twice, and never buffing Dandelion herself
public class WindGust : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float hitboxRadius;
    private float damage;
    private Plant source;
    private float maxDistance;
    private float distanceTraveled;

    private float trapDuration;
    private float hasteBonus;
    private float hasteDuration;
    private float cooldownRefundPercent;

    private readonly HashSet<Insect> hitInsects = new HashSet<Insect>();
    private readonly HashSet<Plant> hitPlants = new HashSet<Plant>();

    [SerializeField] private SpriteRenderer visualRenderer;

    private static readonly DamageTag[] damageTags = { DamageTag.AoE, DamageTag.SkillDamage };

    public void Initialize(Vector2 origin, Vector2 direction, float hitboxSize, float speed, float damage, Plant source, float maxDistance,
        float trapDuration, float hasteBonus, float hasteDuration, float cooldownRefundPercent)
    {
        transform.position = origin;
        this.direction = direction.normalized;
        this.hitboxRadius = hitboxSize * 0.5f;
        this.speed = speed;
        this.damage = damage;
        this.source = source;
        this.maxDistance = maxDistance;
        this.trapDuration = trapDuration;
        this.hasteBonus = hasteBonus;
        this.hasteDuration = hasteDuration;
        this.cooldownRefundPercent = cooldownRefundPercent;

        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        if (visualRenderer != null)
            visualRenderer.transform.localScale = new Vector3(hitboxSize, hitboxSize, 1f);
    }

    private void Update()
    {
        if (source == null || !source.IsAlive) { Destroy(gameObject); return; }

        float step = speed * Time.deltaTime;
        transform.position += (Vector3)(direction * step);
        distanceTraveled += step;

        Vector2 windVelocity = direction * speed;

        List<Insect> insectSnapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in insectSnapshot)
        {
            if (insect == null || !insect.IsAlive || hitInsects.Contains(insect)) continue;
            if (Vector2.Distance(transform.position, insect.transform.position) > hitboxRadius) continue;

            hitInsects.Add(insect);
            insect.Damage(damage, source.damageType, source.elementalType, source, true, damageTags);
            insect.ApplyEffect(new WindDisplacedEffect(insect, trapDuration, source, windVelocity));
        }

        List<Plant> plantSnapshot = new List<Plant>(Plant.allPlants);
        foreach (Plant plant in plantSnapshot)
        {
            if (plant == null || !plant.IsAlive || plant == source || hitPlants.Contains(plant)) continue;
            if (Vector2.Distance(transform.position, plant.transform.position) > hitboxRadius) continue;

            hitPlants.Add(plant);
            plant.ApplyEffect(new PollenHasteEffect(plant, hasteDuration, source, hasteBonus));
            if (cooldownRefundPercent > 0f)
                plant.ReduceSkillCooldown(plant.skillCooldown * cooldownRefundPercent);
        }

        if (distanceTraveled >= maxDistance) Destroy(gameObject);
    }
}
