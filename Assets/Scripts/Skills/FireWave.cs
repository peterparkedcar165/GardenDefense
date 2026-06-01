using UnityEngine;
using System.Collections.Generic;

// Stargazer ultimate: a wall of fire that sweeps across the map in a direction,
// damaging each insect once as it passes
public class FireWave : MonoBehaviour
{
    private Vector2 direction;
    private Vector2 perp;
    private float speed, halfWidth, thickness, damage, travelDistance, burnMultiplier;
    private int flammableStacks;
    private Plant source;
    private Vector2 startPos;
    private readonly HashSet<Insect> _hit = new HashSet<Insect>();

    private static readonly DamageTag[] waveTags = { DamageTag.AoE, DamageTag.SkillDamage };

    public void Initialize(Vector2 startPos, Vector2 direction, float speed, float width,
                           float thickness, float damage, float burnMultiplier, int flammableStacks,
                           float travelDistance, Plant source)
    {
        this.startPos        = startPos;
        this.direction       = direction.normalized;
        this.perp            = new Vector2(-this.direction.y, this.direction.x);
        this.speed           = speed;
        this.halfWidth       = width * 0.5f;
        this.thickness       = thickness;
        this.damage          = damage;
        this.burnMultiplier  = burnMultiplier;
        this.flammableStacks = flammableStacks;
        this.travelDistance  = travelDistance;
        this.source          = source;

        transform.position = startPos;
        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(thickness, width, 1f);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || _hit.Contains(insect)) continue;

            Vector2 to = (Vector2)insect.transform.position - (Vector2)transform.position;
            float along = Mathf.Abs(Vector2.Dot(to, direction));   // distance along the sweep
            float side  = Mathf.Abs(Vector2.Dot(to, perp));        // distance across the wall
            if (along <= thickness * 0.5f && side <= halfWidth)
            {
                // amplified damage against already-burning targets
                float dmg = insect.HasEffect<BurnEffect>() ? damage * burnMultiplier : damage;
                insect.Damage(dmg, DamageType.Magic, ElementalType.Fire, source, true, waveTags);
                (source as Stargazer)?.AddFlammable(insect, flammableStacks);
                _hit.Add(insect);
            }

        }

        if (Vector2.Distance(startPos, transform.position) >= travelDistance)
            Destroy(gameObject);
    }
}
