using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// groundthorn ultimate: pillars of earth erupt one after another along a line, starting in
// front of the plant and emerging outwards. every pillar damages, knocks up and pushes
// insects further along the line, so targets caught early ride the entire wave
public class EarthPillars : MonoBehaviour
{
    private Vector3 origin;
    private Vector2 direction;
    private int pillarCount;
    private float startOffset, spacing, interval, radius, hitboxMultiplier, damageGrowth, damage, knockUpForce, knockbackDistance;
    private bool stunOnMax;
    private int stunThreshold;
    private float stunDuration;
    private GroundThorn source;
    private GameObject pillarVisualPrefab;

    private readonly Dictionary<Insect, int> _hitCounts = new Dictionary<Insect, int>();

    private static readonly DamageTag[] pillarTags = { DamageTag.AoE, DamageTag.SkillDamage };

    private const float VisualFadeIn  = 0.05f;
    private const float VisualHold    = 0.8f;
    private const float VisualFadeOut = 0.7f;

    public void Initialize(Vector3 origin, Vector2 direction, int pillarCount, float startOffset,
                           float spacing, float interval, float radius, float hitboxMultiplier,
                           float damageGrowth, float damage, float knockUpForce, float knockbackDistance,
                           bool stunOnMax, int stunThreshold, float stunDuration, GroundThorn source,
                           GameObject pillarVisualPrefab)
    {
        this.origin             = origin;
        this.direction          = direction.normalized;
        this.pillarCount        = pillarCount;
        this.startOffset        = startOffset;
        this.spacing            = spacing;
        this.interval           = interval;
        this.radius             = radius;
        this.hitboxMultiplier   = hitboxMultiplier;
        this.damageGrowth       = damageGrowth;
        this.damage             = damage;
        this.knockUpForce       = knockUpForce;
        this.knockbackDistance  = knockbackDistance;
        this.stunOnMax          = stunOnMax;
        this.stunThreshold      = stunThreshold;
        this.stunDuration       = stunDuration;
        this.source             = source;
        this.pillarVisualPrefab = pillarVisualPrefab;

        StartCoroutine(Erupt());
    }

    private IEnumerator Erupt()
    {
        for (int i = 1; i <= pillarCount; i++)
        {
            // every pillar deals a set percentage more damage than the previous one
            float damageFactor = Mathf.Pow(1f + damageGrowth, i - 1);
            // first pillar erupts startOffset away from the plant, the rest follow at spacing
            float distance = startOffset + spacing * (i - 1);
            EruptPillar(origin + (Vector3)(direction * distance), damageFactor);
            yield return new WaitForSeconds(interval);
        }
        Destroy(gameObject);
    }

    private void EruptPillar(Vector3 position, float damageFactor)
    {
        if (pillarVisualPrefab != null)
        {
            GameObject visual = Instantiate(pillarVisualPrefab, position, Quaternion.identity);
            // scale the visual so its rendered size exactly matches the hit area
            SpriteRenderer sr = visual.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.bounds.size.x > 0f)
                visual.transform.localScale *= radius * 2f / sr.bounds.size.x;
            visual.AddComponent<SpriteFadeInOut>().Play(VisualFadeIn, VisualHold, VisualFadeOut);
        }

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || insect.team == Team.Friendly) continue;
            // the hitbox is slightly larger than the visual circle
            if (Vector3.Distance(position, insect.transform.position) > radius * hitboxMultiplier) continue;

            insect.Damage(damage * damageFactor, source.damageType, source.elementalType, source, true, pillarTags);
            if (!insect.IsAlive) continue;

            insect.ApplyEffect(new KnockUpEffect(insect, 30f, 1, source, knockUpForce));
            // the push follows the pillar line and lasts as long as the knock up keeps the
            // insect airborne, so both motions blend into a single ballistic arc
            float tenacityScale = Mathf.Sqrt(Mathf.Max(0f, 1f - insect.tenacity));
            float airTime = Mathf.Max(0.15f, 2f * knockUpForce * tenacityScale / Insect.gravity);
            source.PushInsect(insect, direction, knockbackDistance, airTime);
            source.ApplyTileBonus(insect);

            _hitCounts.TryGetValue(insect, out int hits);
            _hitCounts[insect] = ++hits;
            if (stunOnMax && hits == stunThreshold)
                insect.ApplyEffect(new StunEffect(insect, stunDuration, 1, source));
        }
    }
}
