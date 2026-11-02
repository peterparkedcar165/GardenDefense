using UnityEngine;
using System.Collections.Generic;

public class WaterlilyProjectile : Projectile
{
    private Transform bubble1, bubble2, bubble3;
    private float bobPhase1, bobPhase2, bobPhase3;
    private const float bobSpeed = 6f;
    private const float bobAmplitude = 0.12f;

    // the bubble leaves from the waterlily herself. its travel speed eases from 0 up to full
    // projectile speed starting the moment it spawns, so it barely drifts toward the target while
    // the visual rises straight up and eases to a stop at the top. once the rise is done the height
    // eases from the top down to the target's height, landing exactly on it however far it went
    private float _riseHeight;
    private float _riseDuration;
    private float _riseTimer;
    private bool _launched;
    private float _startHeight;
    private float _launchHeight;
    private float _accelDuration;
    private float _accelTimer;
    // a target this close ends the rise early, so the bubble still has room to come down onto it
    // instead of passing over it while still high
    private const float EarlyLaunchDistance = 0.5f;
    private float _traveled;
    private Vector3 _lastBasePos;

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
        _riseHeight = data != null ? data.projectileRiseHeight : 0.8f;
        _riseDuration = Mathf.Max(0.01f, data != null ? data.projectileRiseDuration : 0.3f);
        _accelDuration = data != null ? data.projectileLaunchAccelDuration : 0.6f;
        _accelTimer = 0f;
        speedMultiplier = _accelDuration > 0f ? 0f : 1f;
        _riseTimer = 0f;
        _launched = false;
        _startHeight = VisualHeightOffset;
        // this projectile sets the visual height itself for the whole flight
        holdVisualHeight = true;
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
        // speed eases in over _accelDuration from the moment it spawns, smooth at both ends (no
        // snap at the start or when it reaches full speed)
        if (_accelTimer < _accelDuration)
        {
            _accelTimer = Mathf.Min(_accelTimer + Time.deltaTime, _accelDuration);
            float a = _accelTimer / _accelDuration;
            speedMultiplier = a * a * (3f - 2f * a);
        }
        else speedMultiplier = 1f;

        base.Move();
    }

    // the rise is over: from here the height heads down to the target, measured from this point
    private void Launch()
    {
        _launched = true;
        _launchHeight = VisualHeightOffset;
        _traveled = 0f;
        _lastBasePos = transform.position;
    }

    protected override void OnBaseMoved()
    {
        if (!_launched)
        {
            // rise: the visual goes straight up and eases to a stop at the top while Base drifts
            _riseTimer = Mathf.Min(_riseTimer + Time.deltaTime, _riseDuration);
            float t = _riseTimer / _riseDuration;
            VisualHeightOffset = _startHeight + _riseHeight * (1f - (1f - t) * (1f - t));
            bool targetClose = trackedInsect != null
                && Vector3.Distance(transform.position, trackedInsect.transform.position) < EarlyLaunchDistance;
            if (_riseTimer >= _riseDuration || targetClose) Launch();
            return;
        }

        // lost target: keep the current height and fly straight on, same as every other projectile
        if (trackedInsect == null) return;

        _traveled += Vector3.Distance(_lastBasePos, transform.position);
        _lastBasePos = transform.position;

        // progress along the travel, measured against the target's current spot so it still lands
        // right on a target that keeps moving. eased at both ends, so it leaves the top of the rise
        // without a jolt
        float remaining = Vector3.Distance(transform.position, trackedInsect.transform.position);
        float s = _traveled + remaining > 0.0001f ? _traveled / (_traveled + remaining) : 1f;
        float eased = s * s * (3f - 2f * s);
        float targetHeight = trackedInsect.GetAimPoint().y - trackedInsect.transform.position.y;
        VisualHeightOffset = Mathf.Lerp(_launchHeight, targetHeight, eased);
    }

    // the hit burst travels out exactly as far as her splash radius, with the same fit Calendula's
    // fire burst uses for her attack range. keeps the burst's own authored lifetime
    protected override void ConfigureHitParticles(ParticleSystem ps)
    {
        if (source is Waterlily waterlily)
            ParticleRadiusFit.FitBurstToRadius(ps, waterlily.AoERange, ps.main.startLifetime.constantMax);
    }

    // particle-trail detach-before-destroy is now handled generically by the base class
    // (Projectile.OnBeforeDestroy), for every child particle system automatically
}
