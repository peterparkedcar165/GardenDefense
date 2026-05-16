using UnityEngine;
using System.Collections.Generic;

public class Geyser : MonoBehaviour
{
    public void Initialize(Vector3 position, float radius, float knockDuration, float damage, float knockUpForce, Plant source)
    {
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(position, insect.transform.position) > radius) continue;

            insect.Damage(damage, DamageType.Magic, ElementalType.Water, source, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.SkillDamage });
            insect.ApplyEffect(new KnockUpEffect(insect, 30f, 1, source, knockUpForce));
        }

        Destroy(gameObject, knockDuration + 0.5f);
    }
}
