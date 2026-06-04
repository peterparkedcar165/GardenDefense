using UnityEngine;
using System.Collections.Generic;

// the Ghost Fungus skill: a small rectangle that sweeps in the aimed direction and inflicts
// Fungal Hypnosis on the FIRST insect it touches, then vanishes
public class GhostHypnosisWave : MonoBehaviour
{
    private Vector2 direction, perp;
    private float speed, halfWidth, thickness, travelDistance;
    private Vector2 startPos;
    private Plant source;
    private float healthMultiplier, attackMultiplier, slowPercent, slowDuration;

    public void Initialize(Vector2 startPos, Vector2 direction, float speed, float width, float thickness,
                           float travelDistance, Plant source,
                           float healthMultiplier, float attackMultiplier, float slowPercent, float slowDuration)
    {
        this.startPos         = startPos;
        this.direction        = direction.normalized;
        this.perp             = new Vector2(-this.direction.y, this.direction.x);
        this.speed            = speed;
        this.halfWidth        = width * 0.5f;
        this.thickness        = thickness;
        this.travelDistance   = travelDistance;
        this.source           = source;
        this.healthMultiplier = healthMultiplier;
        this.attackMultiplier = attackMultiplier;
        this.slowPercent      = slowPercent;
        this.slowDuration     = slowDuration;

        transform.position = startPos;
        float angle = Mathf.Atan2(this.direction.y, this.direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        transform.localScale = new Vector3(thickness, width, 1f);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // iterate a copy: FungalHypnotize moves the insect out of allInsects mid-loop
        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            Vector2 to = (Vector2)insect.transform.position - (Vector2)transform.position;
            if (Mathf.Abs(Vector2.Dot(to, direction)) <= thickness * 0.5f &&
                Mathf.Abs(Vector2.Dot(to, perp))      <= halfWidth)
            {
                insect.FungalHypnotize(source, healthMultiplier, attackMultiplier, slowPercent, slowDuration);
                Destroy(gameObject);   // hits only the first insect
                return;
            }
        }

        if (Vector2.Distance(startPos, transform.position) >= travelDistance)
            Destroy(gameObject);
    }
}
