using UnityEngine;
using System.Collections.Generic;

public class Geyser : MonoBehaviour
{
    private Plant source;
    private float radius;
    private bool lingeringMire;
    private float mireTickTimer;
    private const float MireTickInterval = 0.25f;
    private const float MireSlowDuration = 0.5f;
    private const float LingeringMireDuration = 6f;

    public void Initialize(Vector3 position, float radius, float knockDuration, float damage, float knockUpForce, Plant source)
    {
        this.source = source;
        this.radius = radius;
        transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);

        bool tidalSurge = SkillTreeManager.HasUnlock(source, BogIris.TidalSurgeUnlock);
        lingeringMire = SkillTreeManager.HasUnlock(source, BogIris.LingeringMireUnlock);

        List<Insect> snapshot = new List<Insect>(Insect.allInsects);
        foreach (Insect insect in snapshot)
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(position, insect.transform.position) > radius) continue;

            // Tidal Surge: detect whether THIS hit specifically crit by temporarily
            // listening for the crit event around the single Damage call below
            bool crit = false;
            void OnCrit(Entity attacker, Entity target) { if (attacker == source && target == insect) crit = true; }
            if (tidalSurge) Entity.OnCriticalHit += OnCrit;
            insect.Damage(damage, source.damageType, source.elementalType, source, true,
                new DamageTag[] { DamageTag.AoE, DamageTag.SkillDamage, DamageTag.CanHitBurrowed });
            if (tidalSurge) Entity.OnCriticalHit -= OnCrit;

            float force = (tidalSurge && crit) ? knockUpForce * (1f + BogIris.TidalSurgeCritKnockUpBonus) : knockUpForce;
            insect.ApplyEffect(new KnockUpEffect(insect, 30f, 1, source, force));
        }

        // Lingering Mire keeps the geyser around for a fixed 6 seconds instead of the usual
        // brief knock-up window, so its slow has time to actually matter
        Destroy(gameObject, lingeringMire ? LingeringMireDuration : knockDuration + 0.5f);
    }

    private void Update()
    {
        if (source == null || !source.IsAlive) { Destroy(gameObject); return; }
        if (!lingeringMire) return;

        // Lingering Mire: while the Geyser's visual remains, continuously refresh a slow on
        // any ground insect still standing in it
        mireTickTimer += Time.deltaTime;
        if (mireTickTimer < MireTickInterval) return;
        mireTickTimer -= MireTickInterval;

        foreach (Insect insect in new List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || insect is FlyingInsect) continue;
            if (Vector3.Distance(transform.position, insect.transform.position) > radius) continue;
            insect.ApplyEffect(new MireSlowEffect(insect, MireSlowDuration, source));
        }
    }
}
