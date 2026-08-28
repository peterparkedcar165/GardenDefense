using UnityEngine;
using System.Collections.Generic;

public class AnemoneVortex : MonoBehaviour
{
    private float radius;
    private float minPullRadius = 0.4f;
    private float duration;
    private float damagePerTick;
    private float tickInterval;
    private float dragSpeed;
    private float detonationDamage;
    private float detonationRadius;
    private float knockbackForce;
    private bool detonatesOnExpiry;
    private bool isAirborne;
    private Plant source;

    private float tickTimer = 0f;

    private const float AirborneTargetHeight = 0.5f;
    private const float LiftSpeed = 3f;
    private const float LiftEffectDuration = 1f;

    private static readonly DamageTag[] vortexTags     = { DamageTag.AoE, DamageTag.SkillDamage };
    private static readonly DamageTag[] detonationTags = { DamageTag.AoE, DamageTag.SkillDamage };

    [SerializeField] private GameObject detonationBurstPrefab;

    public void Initialize(float radius, float duration, float damagePerTick, float tickInterval,
        float dragSpeed, float detonationDamage, float detonationRadius, float knockbackForce,
        bool detonatesOnExpiry, bool isAirborne, Plant source)
    {
        this.radius            = radius;
        this.duration          = duration;
        this.damagePerTick     = damagePerTick;
        this.tickInterval      = tickInterval;
        this.dragSpeed         = dragSpeed;
        this.detonationDamage  = detonationDamage;
        this.detonationRadius  = detonationRadius;
        this.knockbackForce    = knockbackForce;
        this.detonatesOnExpiry = detonatesOnExpiry;
        this.isAirborne        = isAirborne;
        this.source            = source;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        ParticleSystem particles = GetComponentInChildren<ParticleSystem>();
        if (particles != null)
        {
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.radius = radius;
        }
    }

    private void Update()
    {
        if (source == null || !source.IsAlive) { Destroy(gameObject); return; }

        duration -= Time.deltaTime;
        if (duration <= 0f) { Expire(); return; }

        tickTimer += Time.deltaTime;
        bool doTick = tickTimer >= tickInterval;
        if (doTick) tickTimer -= tickInterval;

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > radius) continue;

            if (Vector3.Distance(transform.position, insect.transform.position) > minPullRadius)
            {
                Vector3 dragStep = Vector3.MoveTowards(
                    insect.transform.position, transform.position, dragSpeed * Time.deltaTime) - insect.transform.position;
                insect.transform.position += Insect.ClampStepAgainstObstacles(insect.transform.position, dragStep);
            }

            if (isAirborne && !insect.isFlying)
            {
                if (insect.visual != null)
                {
                    Vector3 vpos = insect.visual.localPosition;
                    vpos.y = Mathf.MoveTowards(vpos.y, AirborneTargetHeight, LiftSpeed * Time.deltaTime);
                    insect.visual.localPosition = vpos;
                    insect.verticalVelocity = 0f;
                }

                VortexLiftEffect lift = insect.GetEffect<VortexLiftEffect>();
                if (lift == null)
                    insect.ApplyEffect(new VortexLiftEffect(insect, LiftEffectDuration, source));
                else
                    lift.duration = LiftEffectDuration;
            }

            if (!doTick) continue;
            insect.Damage(damagePerTick, source.damageType, source.elementalType, source, false, vortexTags);
        }
    }

    private void Expire()
    {
        if (detonatesOnExpiry)
        {
            SpawnDetonationBurst();

            List<Insect> snapshot = new List<Insect>(Insect.allInsects);
            foreach (Insect insect in snapshot)
            {
                if (insect == null || !insect.IsAlive) continue;
                if (Vector3.Distance(transform.position, insect.transform.position) > detonationRadius) continue;

                insect.Damage(detonationDamage, source.damageType, source.elementalType, source, true, detonationTags);

                Vector2 away = ((Vector2)insect.transform.position - (Vector2)transform.position).normalized;
                if (away.sqrMagnitude < 0.001f) away = Vector2.right;
                float tenacityScale = Mathf.Sqrt(Mathf.Max(0f, 1f - insect.tenacity));
                insect.windVelocity += away * knockbackForce * tenacityScale;
                insect.ApplyEffect(new DisplacedEffect(insect, 0.5f, 1, source));
            }
        }
        Destroy(gameObject);
    }

    private void SpawnDetonationBurst()
    {
        if (detonationBurstPrefab == null) return;
        GameObject obj = Instantiate(detonationBurstPrefab, transform.position, Quaternion.identity);
        ParticleSystem burst = obj.GetComponentInChildren<ParticleSystem>();
        if (burst == null) return;

        ParticleSystem.ShapeModule shape = burst.shape;
        shape.radius = radius;

        // let the engine destroy it once every particle has finished, instead of guessing a delay
        ParticleSystem.MainModule main = burst.main;
        main.stopAction = ParticleSystemStopAction.Destroy;
    }
}
