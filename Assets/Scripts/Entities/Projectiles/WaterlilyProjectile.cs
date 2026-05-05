using UnityEngine;

public class WaterlilyProjectile : Projectile
{
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
        
        Waterlily waterlily = source as Waterlily;

        if (waterlily != null)
        {
            float splashDamage = projectileDamage * (0.5f + 0.05f * waterlily.effectivePath2Level);
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, waterlily.AoERange);

            foreach (Collider2D hit in hits)
            {
                Insect splashedInsect = hit.GetComponent<Insect>();
                if (splashedInsect != null && splashedInsect != insect)
                    splashedInsect.Damage(splashDamage, damageType, elementalType, source, true, new DamageTag[] {DamageTag.AoE, DamageTag.Attack, DamageTag.Projectile});
            }
        }
        if (piercing > 0)
        {
            piercing--;
        } else
        {
            Destroy(gameObject);
        }
    }

    protected override void Move()
    {
        base.Move();
    }  
}
