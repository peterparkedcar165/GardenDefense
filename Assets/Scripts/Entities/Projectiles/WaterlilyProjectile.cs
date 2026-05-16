using UnityEngine;
using System.Collections.Generic;

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
        {
            float splashDamage = projectileDamage * (0.5f + 0.05f * waterlily.effectivePath2Level);
            foreach (Insect splashedInsect in new List<Insect>(Insect.allInsects))
            {
                if (splashedInsect == null || !splashedInsect.IsAlive) continue;
                if (splashedInsect != insect && Vector3.Distance(transform.position, splashedInsect.transform.position) <= waterlily.AoERange)
                    splashedInsect.Damage(splashDamage, damageType, elementalType, source, true, new DamageTag[] {DamageTag.AoE, DamageTag.Attack, DamageTag.Projectile});
            }
        }
    }

    protected override void Move()
    {
        base.Move();
    }  
}
