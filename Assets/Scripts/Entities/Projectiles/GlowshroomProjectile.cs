using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlowshroomProjectile : Projectile
{
    // made once and shared by every hit: nothing writes into it
    private static readonly DamageTag[] HitTags = { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget };

    private Glowshroom _mushroom;

    public void Initialize(Vector3 target, float damage, float speed, float maxRange, int piercing,
                           DamageType damageType, ElementalType elementalType, Glowshroom source)
    {
        base.Initialize(target, damage, speed, maxRange, piercing, damageType, elementalType, source);
        _mushroom     = source;

        Light2D light = gameObject.AddComponent<Light2D>();
        light.lightType              = Light2D.LightType.Point;
        light.color                  = new Color(0.05f, 0.65f, 1f);
        light.intensity              = 1f;
        light.pointLightOuterRadius  = 0.6f;
        light.pointLightInnerRadius  = 0.1f;
    }

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, false,
            HitTags);
        _mushroom?.OnBoltHit(transform.position, insect);
    }
}
