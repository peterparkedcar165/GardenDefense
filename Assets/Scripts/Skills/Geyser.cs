using UnityEngine;
using System.Collections.Generic;

public class Geyser : MonoBehaviour
{
    public void Initialize(Vector3 position, float radius, float knockDuration, float damage, float knockUpHeight, Plant source)
    {
        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(position, insect.transform.position) > radius) continue;

            insect.Damage(damage, DamageType.Magic, ElementalType.Water, source, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.SkillDamage });
            insect.ApplyEffect(new GeyserKnockEffect(insect, knockDuration, 1, source, knockUpHeight));
        }

        Destroy(gameObject, knockDuration + 0.5f);
    }
}
