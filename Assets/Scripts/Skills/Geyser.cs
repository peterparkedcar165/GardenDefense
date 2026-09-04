using UnityEngine;
using System.Collections.Generic;

public class Geyser : MonoBehaviour
{
    private Plant source;

    public void Initialize(Vector3 position, float radius, float knockDuration, float damage, float knockUpForce, Plant source)
    {
        this.source = source;
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(position, insect.transform.position) > radius) continue;

            // CanHitBurrowed: the geyser erupts from underground itself, so it can damage and
            // knock up insects currently burrowed (e.g. mid-transit through an UndergroundTunnel),
            // same as CarrotFurrow
            insect.Damage(damage, source.damageType, source.elementalType, source, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.SkillDamage, DamageTag.CanHitBurrowed });
            insect.ApplyEffect(new KnockUpEffect(insect, 30f, 1, source, knockUpForce));
        }

        Destroy(gameObject, knockDuration + 0.5f);
    }

    private void Update()
    {
        if (source == null || !source.IsAlive) Destroy(gameObject);
    }
}
