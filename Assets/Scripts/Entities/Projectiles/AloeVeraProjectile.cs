using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AloeVeraProjectile : MonoBehaviour
{
    private Transform visual;
    private Transform trackedTarget;
    private Vector3 lastKnownPosition; // updated every frame; used as destination once target dies
    private bool hasTriedRetarget; // only attempt to find a replacement target once

    private float speed;
    private float aoERadius;
    private float healAmount;
    private float tempReduction;
    private bool isHealMode;
    private float damage;
    private DamageType damageType;
    private ElementalType elementalType;
    private AloeVera source;

    private float bobDuration; // set by AloeVera based on attack speed

    [Header("Arc Timing")]
    public float fallTime = 0.28f;   // seconds for the visual to fall from peak to 0

    [Header("Arc Shape")]
    public float arcPeakHeight = 1.5f;

    [Header("Bob Shape")]
    public float bobAmplitude = 0.10f;  // height of the hover oscillation
    public float bobFrequency = 8f;     // oscillations per second

    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
    }

    /// <param name="trackTarget">The transform to home toward (may be a moving insect or plant).</param>
    /// <param name="fallbackPos">Fallback world position if trackTarget is already null at spawn.</param>
    /// <param name="bobDuration">How long the projectile bobs at peak; set by caller based on attack speed.</param>
    public void Initialize(Transform trackTarget, Vector3 fallbackPos,
                           float damage, float speed, float aoERadius,
                           float healAmount, float tempReduction, bool isHealMode,
                           float bobDuration,
                           DamageType damageType, ElementalType elementalType, AloeVera source)
    {
        this.trackedTarget     = trackTarget;
        this.lastKnownPosition = trackTarget != null ? trackTarget.position : fallbackPos;
        this.damage            = damage;
        this.speed             = speed;
        this.aoERadius         = aoERadius;
        this.healAmount        = healAmount;
        this.tempReduction     = tempReduction;
        this.isHealMode        = isHealMode;
        this.bobDuration       = bobDuration;
        this.damageType        = damageType;
        this.elementalType     = elementalType;
        this.source            = source;

        visual = transform.Find("Visual");
        if (visual != null)
        {
            SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 1;
        }

        StartCoroutine(FlyRoutine());
    }

    // Returns the target's current position; caches it so we keep heading there after death.
    private Vector3 GetTargetPosition()
    {
        if (trackedTarget != null && trackedTarget.gameObject.activeInHierarchy)
        {
            lastKnownPosition = trackedTarget.position;
        }
        else
        {
            TryRetarget();
            if (trackedTarget != null && trackedTarget.gameObject.activeInHierarchy)
                lastKnownPosition = trackedTarget.position;
        }
        return lastKnownPosition;
    }

    // if our original target died mid-flight, try once to pick up a fresh valid target of the
    // same kind (heal target for heal-mode shots, enemy insect for attack-mode shots) instead of
    // just riding out to the empty spot where the old target used to be
    private void TryRetarget()
    {
        if (hasTriedRetarget || source == null) return;
        hasTriedRetarget = true;

        GameObject candidate = source.FindLobberTarget();
        if (candidate == null) return;

        bool candidateIsHealable = candidate.GetComponent<Plant>() != null
                                 || (candidate.GetComponent<Insect>() is Insect ins && ins.team == Team.Friendly);

        if (candidateIsHealable == isHealMode)
            trackedTarget = candidate.transform;
    }

    private void HomeTowardTarget()
    {
        transform.position = Vector3.MoveTowards(
            transform.position, GetTargetPosition(), speed * Time.deltaTime);
    }

    private void SetVisualY(float y)
    {
        if (visual != null)
            visual.localPosition = new Vector3(0f, y, 0f);
    }

    private IEnumerator FlyRoutine()
    {
        float elapsed;

        // ── Phase 1: Rise ──────────────────────────────────────────────────────────
        // Root homes at full speed. Visual rises 0 → arcPeakHeight.
        // DISTANCE-BASED: phase does not end until root actually reaches the target,
        // so the bob can only start once we're sitting on top of it.
        float initialDistance = Mathf.Max(Vector3.Distance(transform.position, lastKnownPosition), 0.01f);

        while (true)
        {
            float remaining = Vector3.Distance(transform.position, GetTargetPosition());
            if (remaining <= 0.05f) break;

            HomeTowardTarget();

            // t = how far along the journey we are (0 at spawn, 1 at target)
            float progress = 1f - Mathf.Clamp01(remaining / initialDistance);
            SetVisualY(Mathf.Sin(progress * Mathf.PI * 0.5f) * arcPeakHeight);

            yield return null;
        }

        // Snap root exactly onto the target and lock the visual at peak.
        transform.position = GetTargetPosition();
        SetVisualY(arcPeakHeight);

        // ── Phase 2: Bob ───────────────────────────────────────────────────────────
        // Root continues tracking in case the target is still alive and moving.
        // Visual hovers and bobs gently at arcPeakHeight.
        // For attack-mode projectiles, hold the bob until the target insect is on the ground.
        elapsed = 0f;
        Insect trackedInsect = !isHealMode ? trackedTarget?.GetComponent<Insect>() : null;
        while (elapsed < bobDuration || (trackedInsect != null && !trackedInsect.isOnGround))
        {
            elapsed += Time.deltaTime;

            HomeTowardTarget();
            SetVisualY(arcPeakHeight + Mathf.Sin(elapsed * bobFrequency) * bobAmplitude);

            yield return null;
        }

        // ── Phase 3: Fall ──────────────────────────────────────────────────────────
        // Root homes to last known position (target may already be dead).
        // Visual falls arcPeakHeight → 0.
        elapsed = 0f;
        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallTime);

            HomeTowardTarget();
            SetVisualY(Mathf.Cos(t * Mathf.PI * 0.5f) * arcPeakHeight);

            yield return null;
        }

        // Snap exactly onto final target position and reset the visual offset.
        transform.position = GetTargetPosition();
        SetVisualY(0f);

        Explode();
        Destroy(gameObject);
    }

    // hidden passive: every plant this heal touches (single target or splash alike) has its
    // Scorch cut short by a fixed fraction of Scorch's own original duration
    private const float ScorchReductionOnHeal = 0.33f;

    private void Explode()
    {
        bool path2Maxed = source != null && source.IsPath2Maxed;
        foreach (Plant plant in new List<Plant>(Plant.allPlants))
        {
            if (plant == null || !plant.IsAlive) continue;
            if (Vector3.Distance(transform.position, plant.transform.position) <= aoERadius)
            {
                float bonus = path2Maxed ? plant.MissingHealth * 0.12f : 0f;
                plant.Heal(healAmount + bonus, source);
                plant.temperature = Mathf.Max(plant.temperature - tempReduction, 10f);
                plant.GetEffect<ScorchEffect>()?.ReduceByFraction(ScorchReductionOnHeal);
            }
        }

        // also heal friendly minions caught in the burst (they live in friendlyInsects, not allInsects)
        foreach (Insect ally in new List<Insect>(Insect.friendlyInsects))
        {
            if (ally == null || !ally.IsAlive) continue;
            if (Vector3.Distance(transform.position, ally.transform.position) <= aoERadius)
            {
                float bonus = path2Maxed ? ally.MissingHealth * 0.12f : 0f;
                ally.Heal(healAmount + bonus, source);
            }
        }

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (!insect.isOnGround) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) <= aoERadius)
                insect.Damage(damage, damageType, elementalType, source, true,
                    new DamageTag[] { DamageTag.AoE, DamageTag.Attack, DamageTag.Projectile });
        }
    }
}
