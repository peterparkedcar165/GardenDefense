using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlowingMushroomProjectile : Projectile
{
    private GlowingMushroom _mushroom;
    private float _splashRadius;
    private float _splashMult;
    private Insect _target;

    public void Initialize(Vector3 target, float damage, float speed, float maxRange, int piercing,
                           DamageType damageType, ElementalType elementalType, GlowingMushroom source,
                           float splashRadius, float splashMult)
    {
        base.Initialize(target, damage, speed, maxRange, piercing, damageType, elementalType, source);
        _mushroom     = source;
        _splashRadius = splashRadius;
        _splashMult   = splashMult;

        Light2D light = gameObject.AddComponent<Light2D>();
        light.lightType              = Light2D.LightType.Point;
        light.color                  = new Color(0.05f, 0.65f, 1f);
        light.intensity              = 1.2f;
        light.pointLightOuterRadius  = 0.6f;
        light.pointLightInnerRadius  = 0.1f;
    }

    public void SetTarget(Insect target) => _target = target;

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, false,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
        _mushroom?.OnBoltHit(transform.position, insect);
    }
}
