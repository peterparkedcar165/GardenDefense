using UnityEngine;
using System.Collections.Generic;

public class GerminateEffect : StatusEffect
{
    public static GameObject bloomPrefab;

    private const float BaseAoERadius = 2.5f;
    private const int BaseMaxAoeTargets = 4;
    private readonly float aoeRadius;
    private readonly int maxAoeTargets;
    public float delay = 1f;
    private float cachedelementalAffinity;

    public GerminateEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        cachedelementalAffinity = source?.elementalAffinity ?? 0f;
        // snapshotted here, at application - a Blossoming plant's Germinate keeps the larger
        // radius/target cap even if Blossoming expires before this Germinate detonates
        bool radiusBonus = source?.GetEffect<BlossomingEffect>()?.GrantsGerminateRadiusBonus ?? false;
        aoeRadius = radiusBonus ? BaseAoERadius * BlossomingEffect.GerminateRadiusMultiplier : BaseAoERadius;
        maxAoeTargets = radiusBonus ? BlossomingEffect.GerminateMaxTargetsBonus : BaseMaxAoeTargets;
        effectType = Type.negative;
        elementalType = ElementalType.Grass;
    }

    // 32 × (1 + 275% elemental affinity), snapshotted from the source on apply
    private float ComputeDamage() => 32f * (1f + 2.75f * cachedelementalAffinity);

    public override string GetName() => "<color=green>Germinate</color>";
    public override string GetDescription()
    {
        return $"Detonates in <color=green><b>{delay:F0}s</b></color>. Deals <color=green><b>{ComputeDamage():F0}</b></color> <color=green>Grass</color> Physical damage to nearby insects.";
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Germinate", new Color(0.3f, 1f, 0.2f));
    }

    public override void OnTick(float deltaTime) { }

    readonly DamageTag[] damageTags = new DamageTag[] { DamageTag.AoE, DamageTag.ElementalDebuff };
    public override void OnExpire()
    {
        if (target == null) return;

        if (bloomPrefab != null)
        {
            GameObject burst = Object.Instantiate(bloomPrefab, target.transform.position, Quaternion.identity);
            ParticleSystem ps = burst.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                const float lifetime = 0.25f;

                var main = ps.main;
                main.startLifetime = lifetime;
                main.startSpeed = new ParticleSystem.MinMaxCurve(
                    aoeRadius * 0.7f / lifetime,
                    aoeRadius        / lifetime);

                var lvol = ps.limitVelocityOverLifetime;
                lvol.enabled = true;
                lvol.separateAxes = false;
                lvol.dampen = 0.4f;
                AnimationCurve limitCurve = new AnimationCurve(
                    new Keyframe(0f,   1f, 0f, 0f),
                    new Keyframe(0.5f, 1f, 0f, 0f),
                    new Keyframe(1f,   0f, 0f, 0f)
                );
                lvol.limit = new ParticleSystem.MinMaxCurve(aoeRadius / lifetime, limitCurve);

                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(1f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = new ParticleSystem.MinMaxGradient(gradient);
            }
        }

        float damage = ComputeDamage();
        Vector3 origin = target.transform.position;

        // the insect that actually carried Germinate always detonates on itself unconditionally;
        // everyone else caught in the blast is capped at maxAoeTargets, nearest first
        Insect mainTarget = target as Insect;
        List<Insect> others = new List<Insect>();
        foreach (Insect insect in Insect.allInsects)
        {
            if (insect == null || !insect.IsAlive || insect == mainTarget) continue;
            if (Vector3.Distance(origin, insect.transform.position) <= aoeRadius)
                others.Add(insect);
        }

        if (others.Count > maxAoeTargets)
        {
            others.Sort((a, b) =>
                Vector3.Distance(origin, a.transform.position).CompareTo(Vector3.Distance(origin, b.transform.position)));
            others.RemoveRange(maxAoeTargets, others.Count - maxAoeTargets);
        }

        if (mainTarget != null && mainTarget.IsAlive) others.Add(mainTarget);

        foreach (Insect insect in others)
        {
            if (source != null)
                insect.Damage(damage, DamageType.Physical, ElementalType.Grass, source, source.ElementalReactionCanCrit, damageTags);
            else
                insect.Damage(damage, DamageType.Physical, ElementalType.Grass, damageTags);
        }
    }
}
