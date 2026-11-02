using UnityEngine;
using System.Collections.Generic;

public class WaterlilyProjectile : Projectile
{
    private Transform bubble1, bubble2, bubble3;
    private float bobPhase1, bobPhase2, bobPhase3;
    private const float bobSpeed = 6f;
    private const float bobAmplitude = 0.12f;

    // the bubble appears above the waterlily, drifts up a little while Base already heads for the
    // target, then the normal height convergence glides it down onto the target
    private float _riseTimer;
    private float _riseSpeed;
    // the drift never takes more than this share of the expected flight time, so close targets
    // still leave plenty of flight for the bubble to come back down and land the hit
    private const float MaxRiseShareOfFlight = 0.3f;

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
        bubble1 = transform.Find("Bubble1");
        bubble2 = transform.Find("Bubble2");
        bubble3 = transform.Find("Bubble3");
        bobPhase1 = 0f;
        bobPhase2 = Mathf.PI * 0.66f;
        bobPhase3 = Mathf.PI * 1.33f;

        WaterlilyData data = source != null ? source.data as WaterlilyData : null;
        float spawnHeight   = data != null ? data.projectileSpawnHeight   : 0.6f;
        float riseHeight    = data != null ? data.projectileRiseHeight    : 0.2f;
        float riseDuration  = Mathf.Max(0.01f, data != null ? data.projectileRiseDuration : 0.25f);
        _riseSpeed = riseHeight / riseDuration;

        Vector3 aim = trackedInsect != null ? trackedInsect.transform.position : target;
        float flightTime = projectileSpeed > 0f ? Vector3.Distance(transform.position, aim) / projectileSpeed : 0f;
        _riseTimer = Mathf.Min(riseDuration, MaxRiseShareOfFlight * flightTime);
        holdVisualHeight = _riseTimer > 0f;

        VisualHeightOffset = spawnHeight;
        PlaceVisualAndShadow();
    }

    protected override void Update()
    {
        base.Update();
        bobPhase1 += bobSpeed * Time.deltaTime;
        bobPhase2 += bobSpeed * Time.deltaTime;
        bobPhase3 += bobSpeed * Time.deltaTime;
        SetBobY(bubble1, bobPhase1);
        SetBobY(bubble2, bobPhase2);
        SetBobY(bubble3, bobPhase3);
    }

    private void SetBobY(Transform bubble, float phase)
    {
        if (bubble == null) return;
        Vector3 pos = bubble.localPosition;
        pos.y = Mathf.Sin(phase) * bobAmplitude;
        bubble.localPosition = pos;
    }


    protected override void OnHit(Insect insect) // to change for every plant
    {
        //if (source != null)
        //    insect.RegisterAttacker(source);

        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag[] {DamageTag.SingleTarget, DamageTag.Attack, DamageTag.Projectile});

        Waterlily waterlily = source as Waterlily;

        if (waterlily != null)
        {
            waterlily.ApplyStackingSoak(insect);

            DamageTag[] splashTags = { DamageTag.AoE, DamageTag.PassiveDamage };

            List<Insect> splashTargets = new List<Insect>();
            foreach (Insect splashedInsect in Insect.allInsects)
            {
                if (splashedInsect == null || !splashedInsect.IsAlive || splashedInsect == insect) continue;
                if (Vector3.Distance(transform.position, splashedInsect.transform.position) <= waterlily.AoERange)
                    splashTargets.Add(splashedInsect);
            }

            // splash only hits the nearest N insects (2 base, +1 per attack path level), the
            // main target above is unaffected by this cap, it always takes full damage
            int maxSplashTargets = waterlily.SplashTargets;
            if (splashTargets.Count > maxSplashTargets)
            {
                splashTargets.Sort((a, b) =>
                    Vector3.Distance(transform.position, a.transform.position).CompareTo(Vector3.Distance(transform.position, b.transform.position)));
                splashTargets.RemoveRange(maxSplashTargets, splashTargets.Count - maxSplashTargets);
            }

            foreach (Insect splashedInsect in splashTargets)
            {
                splashedInsect.Damage(waterlily.splashDamage, damageType, elementalType, source, true, splashTags);
                waterlily.ApplyStackingSoak(splashedInsect);
            }
        }
    }

    protected override void Move()
    {
        if (_riseTimer > 0f)
        {
            float step = Mathf.Min(Time.deltaTime, _riseTimer);
            _riseTimer -= Time.deltaTime;
            VisualHeightOffset += _riseSpeed * step;
            holdVisualHeight = _riseTimer > 0f;
        }
        base.Move();
    }

    // particle-trail detach-before-destroy is now handled generically by the base class
    // (Projectile.OnBeforeDestroy), for every child particle system automatically
}
