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
    private float startOffset, spacing, radius, hitboxMultiplier, damage, knockUpForce;
    private float visualScale;
    private Carrot source;
    private GameObject squareVisualPrefab;

    private float traveled;      // distance of the plowing square center from the origin
    private float endDistance;   // center of the final segment
    private float speed;
    private int visualsSpawned;

    public const float MaxLevelGrowthPerSegment = 0.1f;

    private readonly HashSet<Insect> _hit = new HashSet<Insect>();
    private readonly List<Insect> _scratch = new List<Insect>();

    // CanHitBurrowed: the furrow physically overturns the ground it plows through, so it can
    // damage and knock up insects currently burrowed (e.g. mid-transit through an
    // UndergroundTunnel), same as any other attack tagged to reach them
    private static readonly DamageTag[] furrowTags = { DamageTag.AoE, DamageTag.SkillDamage, DamageTag.CanHitBurrowed };


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
                           float interval, float radius, float widthMultiplier, float hitboxMultiplier,
                           float damage, float knockUpForce,
                           Carrot source, GameObject squareVisualPrefab)
    {
        this.origin             = origin;
        this.direction          = direction.normalized;
        this.perp               = new Vector2(-this.direction.y, this.direction.x);
        this.segmentCount       = segmentCount;
        this.startOffset        = startOffset;
        // spacing follows the visual size of the carrot (scaled by width level), so they still
        // tile perfectly edge to edge even as they grow
        this.spacing            = VisualSquareSize(squareVisualPrefab, radius * 2f) * widthMultiplier;
        this.radius             = radius * widthMultiplier;
        this.visualScale        = widthMultiplier;
        this.hitboxMultiplier   = hitboxMultiplier;
        this.damage             = damage;
        this.knockUpForce       = knockUpForce;
        this.source             = source;
        this.squareVisualPrefab = squareVisualPrefab;

        traveled    = startOffset;
        speed       = spacing / Mathf.Max(0.01f, interval);   // same pace the old pillar wave had
        endDistance = startOffset + spacing * (segmentCount - 1);
        transform.position = origin;
    }

    // path3 max: each carrot further down the line hits harder and covers more ground than the
    // last, scaled off the FIRST carrot (1st = base, 2nd = +10%, 3rd = +20%, ...) rather than
    // compounding off the previous one, so it's a flat linear ramp rather than exponential
    private float SegmentScale(int segmentIndex) =>
        source.IsPath3Maxed ? 1f + MaxLevelGrowthPerSegment * segmentIndex : 1f;

    private void Update()
    {
        traveled += speed * Time.deltaTime;

        // the hitting square stops at the final segment, travel continues so coverage completes
        float hitTravel = Mathf.Min(traveled, endDistance);
        int segmentIndex = spacing > 0f ? Mathf.Clamp(Mathf.RoundToInt((hitTravel - startOffset) / spacing), 0, segmentCount - 1) : 0;
        float segmentScale = SegmentScale(segmentIndex);

        Vector3 center = origin + (Vector3)(direction * hitTravel);
        float scaledRadius = radius * segmentScale;
        float half = scaledRadius * hitboxMultiplier;

        _scratch.Clear();
        _scratch.AddRange(Insect.allInsects);
        foreach (Insect insect in _scratch)
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly || _hit.Contains(insect)) continue;

            Vector2 to = (Vector2)insect.transform.position - (Vector2)center;
            float along = Vector2.Dot(to, direction);
            float across = Vector2.Dot(to, perp);
            if (Mathf.Abs(along) > half) continue;
            if (Mathf.Abs(across) > half) continue;

            _hit.Add(insect);

            insect.Damage(damage * segmentScale, source.damageType, source.elementalType, source, true, furrowTags);
            if (!insect.IsAlive) continue;

            insect.ApplyEffect(new KnockUpEffect(insect, 30f, 1, source, knockUpForce));
            // pushed sideways, away from the line of carrots (whichever side of it the insect is
            // already on), a distance equal to this carrot's own width, rather than further
            // along the furrow's own direction of travel. the push lasts as long as the knock
            // up keeps the insect airborne, so both motions blend into a single ballistic arc
            Vector2 pushDir = perp * Mathf.Sign(across);
            float pushDistance = scaledRadius * 2f;
            float tenacityScale = Mathf.Sqrt(Mathf.Max(0f, 1f - insect.tenacity));
            float airTime = Mathf.Max(0.15f, 2f * knockUpForce * tenacityScale / Insect.gravity);
            source.PushInsect(insect, pushDir, pushDistance, airTime);
        }

        // spawn each segment's visual square the moment the plow enters it,
        // so the squares appear in sync with the hits happening inside them
        while (visualsSpawned < segmentCount)
        {
            float segmentCenter = startOffset + spacing * visualsSpawned;
            if (traveled < segmentCenter - spacing * 0.5f) break;
            SpawnSquareVisual(origin + (Vector3)(direction * segmentCenter), SegmentScale(visualsSpawned));
            visualsSpawned++;
        }

        if (visualsSpawned >= segmentCount && traveled >= endDistance + spacing * 0.5f)
            Destroy(gameObject);
    }

    private void SpawnSquareVisual(Vector3 position, float segmentScale)
    {
        if (squareVisualPrefab == null) return;
        // spawned at its authored size (scaled by the current width level and, at path3 max,
        // this segment's own growth) and upright, carrots always render right side up. a small
        // random offset keeps the row from looking machine planted
        float jitter = source.VisualPositionJitter;
        position += new Vector3(Random.Range(-jitter, jitter), Random.Range(-jitter, jitter), 0f);
        GameObject visual = Instantiate(squareVisualPrefab, position, Quaternion.identity);
        visual.transform.localScale *= visualScale * segmentScale;
        visual.AddComponent<SpriteFadeInOut>().Play(source.VisualFadeIn, source.VisualHold, source.VisualFadeOut);
    }
}
