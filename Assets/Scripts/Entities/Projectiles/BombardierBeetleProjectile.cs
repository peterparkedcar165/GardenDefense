using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// freestanding lobbed projectile (not a Projectile subclass - that base class is plant-only and
// travels in a straight line with no arc). flies from the beetle to a fixed impact position set
// at fire time, then resolves damage there regardless of whether the primary target is still
// alive - this is the "shoots at the locked position/empty space" behavior from BombardierBeetle
public class BombardierBeetleProjectile : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private float arcPeakHeight = 1.5f;

    private Vector3 startPos, targetPos;
    private Plant primaryTarget;
    private float primaryDamage, splashDamage, splashRadius, scorchChance, scorchDuration, travelDuration;
    private DamageType damageType;
    private ElementalType elementalType;
    private Entity source;

    public void Initialize(Vector3 start, Vector3 target, Plant primaryTarget,
        float primaryDamage, float splashDamage, float splashRadius,
        float scorchChance, float scorchDuration,
        DamageType damageType, ElementalType elementalType, Entity source, float projectileSpeed)
    {
        startPos = start;
        targetPos = target;
        this.primaryTarget = primaryTarget;
        this.primaryDamage = primaryDamage;
        this.splashDamage = splashDamage;
        this.splashRadius = splashRadius;
        this.scorchChance = scorchChance;
        this.scorchDuration = scorchDuration;
        this.damageType = damageType;
        this.elementalType = elementalType;
        this.source = source;
        travelDuration = Vector3.Distance(start, target) / Mathf.Max(0.01f, projectileSpeed);

        transform.position = start;
        StartCoroutine(FlyRoutine());
    }

    private IEnumerator FlyRoutine()
    {
        float elapsed = 0f;
        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            if (visual != null)
            {
                Vector3 vp = visual.localPosition;
                vp.y = Mathf.Sin(t * Mathf.PI) * arcPeakHeight;
                visual.localPosition = vp;
            }
            yield return null;
        }

        transform.position = targetPos;
        Impact();
        Destroy(gameObject);
    }

    private void Impact()
    {
        if (primaryTarget != null && primaryTarget.IsAlive)
            DealDamage(primaryTarget, primaryDamage, DamageTag.SingleTarget);

        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive || plant == primaryTarget) continue;
            if (Vector3.Distance(plant.transform.position, targetPos) > splashRadius) continue;
            DealDamage(plant, splashDamage, DamageTag.AoE);
        }
    }

    private void DealDamage(Plant plant, float damage, DamageTag rangeTag)
    {
        plant.Damage(damage, damageType, elementalType, source, false,
            new DamageTag[] { DamageTag.Attack, DamageTag.Projectile, rangeTag });

        if (plant.IsAlive && Random.value < scorchChance)
            plant.ApplyEffect(new ScorchEffect(plant, scorchDuration, 1, source));
    }
}
