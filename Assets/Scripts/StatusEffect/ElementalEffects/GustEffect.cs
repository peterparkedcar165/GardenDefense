using System.Collections;
using UnityEngine;

public class GustEffect : ElementalDebuff
{
    private const float WindshearRadius = 1.5f;

    public static event System.Action<Vector3, Entity> OnWindshearTriggered;

    public GustEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Wind;
    }

    public override string GetName() => "<color=#B2EBF2>Gust</color>";
    public override string GetDescription()
    {
        float halfDamage = 72f * (1f + 1.8f * source.elementalAffinity);
        return $"Reacts with an existing primer, dealing <color=#E0E0E0><b>{halfDamage:F0}</b></color> Wind + <color=#E0E0E0><b>{halfDamage:F0}</b></color> elemental Magic damage to all insects within a <color=#E0E0E0><b>{WindshearRadius:F1}</b></color> radius. (72 × (1 + 1.8× <color=#FFD700>{source.elementalAffinity * 100:F0}% Elemental Affinity</color>))";
    }

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<BlazeEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Fire));
            return;
        }
        if (insect.HasEffect<ColdEffect>())
        {
            insect.RemoveEffect<ColdEffect>();
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Ice));
            return;
        }
        if (insect.HasEffect<WetEffect>())
        {
            insect.RemoveEffect<WetEffect>();
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Water));
            return;
        }
        if (insect.HasEffect<TaintedEffect>())
        {
            insect.RemoveEffect<TaintedEffect>();
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Poison));
            return;
        }
        if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<SproutEffect>();
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Nature));
            return;
        }

        insect.RemoveEffect<GustEffect>();
    }

    private static IEnumerator WindshearDelay(Insect target, Entity source, ElementalType primerElement)
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Windshear", new Color(0.85f, 1f, 0.85f));
        // detonate around where the reaction happened, so a dying target does not cancel the AoE
        Vector3 center = target.transform.position;
        float halfDamage = 72f * (1f + 1.8f * source.elementalAffinity);
        DamageTag[] tags = new DamageTag[] { DamageTag.ElementalDebuff };

        yield return new WaitForSeconds(0.1f);
        DealWindshearAoe(center, halfDamage, ElementalType.Wind, source, tags);
        yield return new WaitForSeconds(0.05f);
        DealWindshearAoe(center, halfDamage, primerElement, source, tags);

        OnWindshearTriggered?.Invoke(center, source);
    }

    // deals one burst of the reaction damage to every insect within WindshearRadius of the center
    private static void DealWindshearAoe(Vector3 center, float damage, ElementalType element, Entity source, DamageTag[] tags)
    {
        foreach (Insect insect in new System.Collections.Generic.List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive) continue;
            if (Vector3.Distance(center, insect.transform.position) <= WindshearRadius)
                insect.Damage(damage, DamageType.Magic, element, source, source?.ElementalReactionCanCrit ?? false, tags);
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
