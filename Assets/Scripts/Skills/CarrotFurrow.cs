using UnityEngine;
using System.Collections.Generic;

// carrot ultimate: a square of churned earth plows along a line from the plant towards
// the target, damaging each insect exactly once as it passes over them. as the square
// enters each segment of the line it sprouts a carrot visual there
public class CarrotFurrow : MonoBehaviour
{
    private Vector3 origin;
    private Vector2 direction, perp;
    private int segmentCount;
    private float startOffset, spacing, radius, hitboxMultiplier, damage, knockUpForce, knockbackDistance;
    private Carrot source;
    private GameObject squareVisualPrefab;

    private float traveled;      // distance of the plowing square center from the origin
    private float endDistance;   // center of the final segment
    private float speed;
    private int visualsSpawned;

    private readonly HashSet<Insect> _hit = new HashSet<Insect>();
    private readonly List<Insect> _scratch = new List<Insect>();

    private static readonly DamageTag[] furrowTags = { DamageTag.AoE, DamageTag.SkillDamage };


    // the rendered world size of the carrot visual at its authored prefab scale.
    // spacing derives from this so the carrots always tile perfectly edge to edge
    public static float VisualSquareSize(GameObject visualPrefab, float fallback)
    {
        if (visualPrefab == null) return fallback;
        SpriteRenderer sr = visualPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return fallback;
        float size = sr.sprite.bounds.size.x * sr.transform.lossyScale.x;
        return size > 0f ? size : fallback;
    }

    public void Initialize(Vector3 origin, Vector2 direction, int segmentCount, float startOffset,
                           float interval, float radius, float hitboxMultiplier,
                           float damage, float knockUpForce, float knockbackDistance,
                           Carrot source, GameObject squareVisualPrefab)
    {
        this.origin             = origin;
        this.direction          = direction.normalized;
        this.perp               = new Vector2(-this.direction.y, this.direction.x);
        this.segmentCount       = segmentCount;
        this.startOffset        = startOffset;
        // spacing follows the visual size of the carrot, so they tile perfectly
        this.spacing            = VisualSquareSize(squareVisualPrefab, radius * 2f);
        this.radius             = radius;
        this.hitboxMultiplier   = hitboxMultiplier;
        this.damage             = damage;
        this.knockUpForce       = knockUpForce;
        this.knockbackDistance  = knockbackDistance;
        this.source             = source;
        this.squareVisualPrefab = squareVisualPrefab;

        traveled    = startOffset;
        speed       = spacing / Mathf.Max(0.01f, interval);   // same pace the old pillar wave had
        endDistance = startOffset + spacing * (segmentCount - 1);
        transform.position = origin;
    }

    private void Update()
    {
        traveled += speed * Time.deltaTime;

        // the hitting square stops at the final segment, travel continues so coverage completes
        float hitTravel = Mathf.Min(traveled, endDistance);
        Vector3 center = origin + (Vector3)(direction * hitTravel);
        float half = radius * hitboxMultiplier;

        _scratch.Clear();
        _scratch.AddRange(Insect.allInsects);
        foreach (Insect insect in _scratch)
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly || _hit.Contains(insect)) continue;

            Vector2 to = (Vector2)insect.transform.position - (Vector2)center;
            if (Mathf.Abs(Vector2.Dot(to, direction)) > half) continue;
            if (Mathf.Abs(Vector2.Dot(to, perp)) > half) continue;

            _hit.Add(insect);

            insect.Damage(damage, source.damageType, source.elementalType, source, true, furrowTags);
            if (!insect.IsAlive) continue;

            insect.ApplyEffect(new KnockUpEffect(insect, 30f, 1, source, knockUpForce));
            // the push follows the furrow and lasts as long as the knock up keeps the
            // insect airborne, so both motions blend into a single ballistic arc
            float tenacityScale = Mathf.Sqrt(Mathf.Max(0f, 1f - insect.tenacity));
            float airTime = Mathf.Max(0.15f, 2f * knockUpForce * tenacityScale / Insect.gravity);
            source.PushInsect(insect, direction, knockbackDistance, airTime);
            source.ApplyTileBonus(insect);
        }

        // spawn each segment's visual square the moment the plow enters it,
        // so the squares appear in sync with the hits happening inside them
        while (visualsSpawned < segmentCount)
        {
            float segmentCenter = startOffset + spacing * visualsSpawned;
            if (traveled < segmentCenter - spacing * 0.5f) break;
            SpawnSquareVisual(origin + (Vector3)(direction * segmentCenter));
            visualsSpawned++;
        }

        if (visualsSpawned >= segmentCount && traveled >= endDistance + spacing * 0.5f)
            Destroy(gameObject);
    }

    private void SpawnSquareVisual(Vector3 position)
    {
        if (squareVisualPrefab == null) return;
        // spawned at its authored size and upright, carrots always render right side up.
        // a small random offset keeps the row from looking machine planted
        float jitter = source.VisualPositionJitter;
        position += new Vector3(Random.Range(-jitter, jitter), Random.Range(-jitter, jitter), 0f);
        GameObject visual = Instantiate(squareVisualPrefab, position, Quaternion.identity);
        visual.AddComponent<SpriteFadeInOut>().Play(source.VisualFadeIn, source.VisualHold, source.VisualFadeOut);
    }
}
