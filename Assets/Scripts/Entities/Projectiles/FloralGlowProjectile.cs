using UnityEngine;

// Floral Glow's own delivery projectile: fired from Calendula straight toward the targeted
// plant when the skill is cast, instead of the buff applying instantly. the Floral Glow effect
// itself is only ever granted once this actually arrives - a dead target or a dead Calendula
// before then simply lets the cast fizzle, same as the buff never having been cast at all
public class FloralGlowProjectile : MonoBehaviour
{
    [SerializeField] private GameObject hitParticlePrefab;
    // the sprite's own default facing angle (degrees) - e.g. 180 if it's drawn facing left,
    //0 if facing right, 90/-90 if up/down. adjust to match whatever art this prefab uses
    [SerializeField] private float spriteDefaultFacingAngle = 180f;

    private Calendula _calendula;
    private Plant _target;
    private float _duration;
    private int _level;
    private float _speed;

    private const float ArrivalThreshold = 0.15f;
    private const float LightRadius = 1f;
    private const float LightFadeOutDuration = 0.5f;

    private LightFader _fader;
    // the light lives on its own child object (rather than directly on this one) so it can be
    // detached and left to fade out on its own the instant this projectile is destroyed, instead
    // of the light's fade-out time delaying the projectile's own destruction
    private GameObject _lightObj;

    // speed comes from CalendulaData.floralGlowProjectileSpeed, not a fixed value on this prefab,
    // so it's tunable the same way every other Floral Glow number is
    public void Initialize(Calendula calendula, Plant target, float duration, int level, float speed)
    {
        _calendula = calendula;
        _target = target;
        _duration = duration;
        _level = level;
        _speed = speed;
    }

    private void Awake()
    {
        if (DarknessManager.instance == null) return;

        _lightObj = new GameObject("FloralGlowLight");
        _lightObj.transform.SetParent(transform, false);
        _lightObj.transform.localPosition = Vector3.zero;

        var light = _lightObj.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
        light.color = Color.white;
        light.intensity = 0f;
        light.falloffIntensity = 0.5f;
        light.pointLightOuterRadius = LightRadius;
        light.pointLightInnerRadius = LightRadius * 0.3f;

        _fader = _lightObj.AddComponent<LightFader>();
        _fader.Setup(light, 1f);
        _fader.FadeIn(0.3f);

        DarknessManager.RegisterLightSource(_lightObj.transform, LightRadius);
    }

    // the projectile itself (sprite, movement, etc.) is destroyed immediately - only the light
    // is detached first and left behind to fade out and destroy itself on its own schedule
    private void DestroyProjectile()
    {
        if (_lightObj != null)
        {
            Transform lt = _lightObj.transform;
            Vector3 worldPos = lt.position;
            Quaternion worldRot = lt.rotation;
            lt.SetParent(null, false);
            lt.position = worldPos;
            lt.rotation = worldRot;

            DarknessManager.UnregisterLightSource(lt);
            if (_fader != null) _fader.FadeOut(LightFadeOutDuration, destroyOnComplete: true);
            else Destroy(_lightObj);
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        if (_calendula == null || !_calendula.IsAlive || _target == null || !_target.IsAlive)
        {
            DestroyProjectile();
            return;
        }

        Vector3 destination = _target.transform.position;
        Vector3 toDestination = destination - transform.position;
        if (toDestination.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(toDestination.y, toDestination.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle - spriteDefaultFacingAngle);
        }

        transform.position = Vector3.MoveTowards(transform.position, destination, _speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, destination) > ArrivalThreshold) return;

        Arrive();
    }

    private void Arrive()
    {
        // two Calendulas can both target the same plant before either projectile lands (both
        // casts validated fine at cast time, since neither Floral Glow existed yet) - the first
        // one to actually arrive wins. this one simply refunds its own Calendula's cooldown and
        // fizzles with no effect and no hit particles, instead of overwriting the winner's buff
        FloralGlowEffect existing = _target.GetEffect<FloralGlowEffect>();
        if (existing != null && existing.source != _calendula)
        {
            _calendula.skillCooldownTimer = 0f;
            DestroyProjectile();
            return;
        }

        PlayHitParticles();
        _target.ApplyEffect(new FloralGlowEffect(_target, _duration, _level, _calendula, _calendula));
        DestroyProjectile();
    }

    // one-shot burst at the point of arrival - optional, left null if no particle is assigned yet
    private void PlayHitParticles()
    {
        if (hitParticlePrefab == null) return;

        GameObject obj = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
        ParticleSystem ps = obj.GetComponent<ParticleSystem>();
        float lifetime = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f;
        Destroy(obj, lifetime);
    }
}
