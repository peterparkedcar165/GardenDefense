using UnityEngine;
using System.Collections.Generic;

public class WaterlilyProjectile : Projectile
{
    private Transform bubble1, bubble2, bubble3;
    private float bobPhase1, bobPhase2, bobPhase3;
    private const float bobSpeed = 6f;
    private const float bobAmplitude = 0.12f;

    // the bubble leaves from the waterlily herself and arcs to its target. Base travels straight
    // along the ground at full speed while the visual follows one smooth curve: it shoots up first,
    // peaks a third of the way there, and lands exactly on the target's height on arrival
    private float _arcHeight;
    private float _startHeight;
    private float _traveled;
    private Vector3 _lastBasePos;
    // 27/4 * s * (1 - s)^2 peaks at exactly 1 when s is a third of the way, and is 0 at both ends
    private const float ArcShapeScale = 6.75f;

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
        _arcHeight = data != null ? data.projectileArcHeight : 0.8f;
        _startHeight = VisualHeightOffset;
        _traveled = 0f;
        _lastBasePos = transform.position;
        // the arc owns the visual height for the whole flight, Base keeps full ground speed
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
        base.Move();
    }

    protected override void OnBaseMoved()
    {
        // lost target: keep the current height and fly straight on, same as every other projectile
        if (trackedInsect == null) return;

        _traveled += Vector3.Distance(_lastBasePos, transform.position);
        _lastBasePos = transform.position;

        // progress along the flight, measured against the target's current spot so it still lands
        // right on a target that keeps moving
        float remaining = Vector3.Distance(transform.position, trackedInsect.transform.position);
        float s = _traveled + remaining > 0.0001f ? _traveled / (_traveled + remaining) : 1f;
        float targetHeight = trackedInsect.GetAimPoint().y - trackedInsect.transform.position.y;
        float arc = ArcShapeScale * s * (1f - s) * (1f - s);
        VisualHeightOffset = Mathf.Lerp(_startHeight, targetHeight, s) + _arcHeight * arc;
    }

    // particle-trail detach-before-destroy is now handled generically by the base class
    // (Projectile.OnBeforeDestroy), for every child particle system automatically
}
