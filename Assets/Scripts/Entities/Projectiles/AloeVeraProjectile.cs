using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AloeVeraProjectile : MonoBehaviour
{
    private Transform visual;
    private Vector3 targetPosition;
    private float speed;
    private float aoERadius;
    private float healAmount;
    private float tempReduction;
    private bool isHealMode;
    private float damage;
    private DamageType damageType;
    private ElementalType elementalType;
    private AloeVera source;

    public float minFlightDuration = 0.5f;

    public float arcPeakHeight = 1.2f;
    private const float arcRiseEnd = 0.5f;

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    public void Initialize(Vector3 target, float damage, float speed, float aoERadius,
                           float healAmount, float tempReduction, bool isHealMode,
                           DamageType damageType, ElementalType elementalType, AloeVera source)
    {
        this.targetPosition = target;
        this.damage         = damage;
        this.speed          = speed;
        this.aoERadius      = aoERadius;
        this.healAmount     = healAmount;
        this.tempReduction  = tempReduction;
        this.isHealMode     = isHealMode;
        this.damageType     = damageType;
        this.elementalType  = elementalType;
        this.source         = source;
        visual = transform.Find("Visual");

        if (visual != null)
        {
            SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 1;
        }

        StartCoroutine(FlyRoutine());
    }

    private IEnumerator FlyRoutine()
    {
        float totalDistance = Mathf.Max(Vector3.Distance(transform.position, targetPosition), 0.01f);
        float actualSpeed = Mathf.Min(speed, totalDistance / Mathf.Max(minFlightDuration, 0.01f));

        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, actualSpeed * Time.deltaTime);

            if (visual != null)
            {
                float progress = 1f - (Vector3.Distance(transform.position, targetPosition) / totalDistance);
                float arcY;
                if (progress < arcRiseEnd)
                    arcY = Mathf.Sin(progress / arcRiseEnd * Mathf.PI * 0.5f) * arcPeakHeight;
                else
                    arcY = Mathf.Cos((progress - arcRiseEnd) / (1f - arcRiseEnd) * Mathf.PI * 0.5f) * arcPeakHeight;
                Vector3 pos = visual.localPosition;
                pos.y = arcY;
                visual.localPosition = pos;
            }

            yield return null;
        }

        transform.position = targetPosition;
        if (visual != null)
            visual.localPosition = Vector3.zero;

        Explode();
        Destroy(gameObject);
    }

    private void Explode()
    {
        if (isHealMode)
        {
            foreach (Plant plant in new List<Plant>(Plant.allPlants))
            {
                if (plant == null || !plant.IsAlive) continue;
                if (Vector3.Distance(transform.position, plant.transform.position) <= aoERadius)
                {
                    plant.Heal(healAmount);
                    plant.temperature = Mathf.Max(plant.temperature - tempReduction, 10f);
                }
            }
        }
        else
        {
            foreach (Insect insect in new List<Insect>(Insect.allInsects))
            {
                if (insect == null || !insect.IsAlive) continue;
                if (Vector3.Distance(transform.position, insect.transform.position) <= aoERadius)
                    insect.Damage(damage, damageType, elementalType, source, true,
                        new DamageTag[] { DamageTag.AoE, DamageTag.Attack, DamageTag.Projectile });
            }
        }
    }
}
