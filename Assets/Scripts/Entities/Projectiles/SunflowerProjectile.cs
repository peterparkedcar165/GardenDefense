using UnityEngine;

public class SunflowerProjectile : Projectile
{
    [SerializeField] private GameObject hitParticlePrefab;

    public override void Initialize(Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
    }

    protected override void Update()
    {
        base.Update();
    }


    protected override void OnHit(Insect insect) // to change for every plant
    {
        //if (source != null)
        //    insect.RegisterAttacker(source);

        insect.Damage(projectileDamage, damageType, elementalType, source, true, new DamageTag[] {DamageTag.SingleTarget, DamageTag.Attack, DamageTag.Projectile});
        
        Sunflower sunflower = source as Sunflower;

        if (sunflower != null)
            sunflower.ReduceSunTimer();

        PlayHitParticles();
    }

    // one-shot burst at the point of impact - this prefab plays on its own (Play On Awake,
    // non-looping) but doesn't self-destroy (Stop Action: None), so it has to be cleaned up
    // manually once it's done, sized to whatever duration/lifetime are actually set on it
    private void PlayHitParticles()
    {
        if (hitParticlePrefab == null) return;

        GameObject obj = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
        Destroy(obj, lifetime);
    }

    protected override void Move()
    {
        base.Move();

        // sprite art faces left by default (angle 180), so the rotation needed to point it along
        // the current direction is the direction's own angle minus that 180 offset
        if (direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 180f);
        }
    }

    // called by the base class right before Destroy(gameObject), while the child particle trail
    // is still fully intact - detaching it here (rather than reacting in OnDestroy, which runs
    // too late since Unity destroys children along with the parent) lets it survive and finish
    // fading out naturally instead of popping out of existence with the projectile
    protected override void OnBeforeDestroy()
    {
        ParticleSystem trail = GetComponentInChildren<ParticleSystem>();
        if (trail == null) return;

        // SetParent(null, true) would preserve world position by rewriting localScale to cancel
        // out the parent's own scale - but this particle system's Scaling Mode is Local, which
        // reads localScale directly as the particle-size multiplier, so that rewrite makes every
        // particle instantly snap to a smaller size the moment it detaches. reparenting with
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
