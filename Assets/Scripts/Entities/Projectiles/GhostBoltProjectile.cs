using UnityEngine.Rendering.Universal;

// the Ghost Fungus' attack: a spectral bolt that damages the first insect hit
public class GhostBoltProjectile : Projectile
{
    public override void Initialize(UnityEngine.Vector3 target, float projectileDamage, float projectileSpeed, float maxRange, int piercing, DamageType damageType, ElementalType elementalType, Shooter source)
    {
        base.Initialize(target, projectileDamage, projectileSpeed, maxRange, piercing, damageType, elementalType, source);
        Light2D light = gameObject.AddComponent<Light2D>();
        light.lightType             = Light2D.LightType.Point;
        light.color                 = new UnityEngine.Color(0.05f, 0.65f, 1f);
        light.intensity             = 1f;
        light.pointLightOuterRadius = 0.6f;
        light.pointLightInnerRadius = 0.1f;
    }

    protected override void OnHit(Insect insect)
    {
        insect.Damage(projectileDamage, damageType, elementalType, source, true,
            new DamageTag[] { DamageTag.Projectile, DamageTag.Attack, DamageTag.SingleTarget });
    }
}
