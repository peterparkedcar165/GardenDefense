using UnityEngine;

public class WaterlilyProjectile : Projectile
{
    private Transform bubble1, bubble2, bubble3;
    private float bobPhase1, bobPhase2, bobPhase3;
    private const float bobSpeed = 6f;
    private const float bobAmplitude = 0.12f;

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
        bubble1 = transform.Find("Bubble1");
        bubble2 = transform.Find("Bubble2");
        bubble3 = transform.Find("Bubble3");
        bobPhase1 = 0f;
        bobPhase2 = Mathf.PI * 0.66f;
        bobPhase3 = Mathf.PI * 1.33f;
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
            waterlily.ApplyStackingSlow(insect);
    }

    protected override void Move()
    {
        base.Move();
    }

    // called by the base class right before Destroy(gameObject), while the child particle trail
    // is still fully intact - detaching it here (rather than reacting in OnDestroy, which runs
    // too late since Unity destroys children along with the parent) lets it survive and finish
    // fading out naturally instead of popping out of existence with the projectile. identical to
    // SunflowerProjectile/CalendulaProjectile's version
    protected override void OnBeforeDestroy()
    {
        ParticleSystem trail = GetComponentInChildren<ParticleSystem>();
        if (trail == null) return;

        // SetParent(null, true) would preserve world position by rewriting localScale to cancel
        // out the parent's own scale - but if this particle system's Scaling Mode is Local, that
        // reads localScale directly as the particle-size multiplier, so the rewrite would make
        // every particle instantly snap to a smaller size the moment it detaches. reparenting with
        // worldPositionStays: false leaves localScale untouched, so position/rotation have to be
        // restored manually instead
        Transform t = trail.transform;
        Vector3 worldPos = t.position;
        Quaternion worldRot = t.rotation;

        t.SetParent(null, false);
        t.position = worldPos;
        t.rotation = worldRot;

        trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(trail.gameObject, trail.main.startLifetime.constantMax);
    }
}
