using System.Collections;
using UnityEngine;

public class WindPrimer : ElementalDebuff
{
    private const float WindshearRadius = 1.5f;

    public static event System.Action<Vector3, Entity> OnWindshearTriggered;

    public WindPrimer(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        elementalType = ElementalType.Wind;
    }

    public override string GetName() => "<color=#B2EBF2>Wind</color>";
    public override string GetDescription()
    {
        float halfDamage = 52f * (1f + 0.6f * source.elementalAffinity);
        return $"Reacts with an existing primer, dealing <color=#E0E0E0><b>{halfDamage:F0}</b></color> Wind + <color=#E0E0E0><b>{halfDamage:F0}</b></color> elemental Magic damage to all insects within a <color=#E0E0E0><b>{WindshearRadius:F1}</b></color> radius. (52 × (1 + 0.6× <color=#FFD700>{source.elementalAffinity * 100:F0}% Elemental Affinity</color>))";
    }

    protected override void OnPrimerApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<FirePrimer>())
        {
            insect.RemoveEffect<FirePrimer>();
            insect.RemoveEffect<WindPrimer>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Fire));
            return;
        }
        if (insect.HasEffect<IcePrimer>())
        {
            insect.RemoveEffect<IcePrimer>();
            insect.RemoveEffect<WindPrimer>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Ice));
            return;
        }
        if (insect.HasEffect<WaterPrimer>())
        {
            insect.RemoveEffect<WaterPrimer>();
            insect.RemoveEffect<WindPrimer>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Water));
            return;
        }
        if (insect.HasEffect<PoisonPrimer>())
        {
            insect.RemoveEffect<PoisonPrimer>();
            insect.RemoveEffect<WindPrimer>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Poison));
            return;
        }
        if (insect.HasEffect<GrassPrimer>())
        {
            insect.RemoveEffect<GrassPrimer>();
            insect.RemoveEffect<WindPrimer>();
            insect.StartCoroutine(WindshearDelay(insect, source, ElementalType.Grass));
            return;
        }

        insect.RemoveEffect<WindPrimer>();
    }

    private static IEnumerator WindshearDelay(Insect target, Entity source, ElementalType primerElement)
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Windshear", new Color(0.85f, 1f, 0.85f));
        // detonate around where the reaction happened, so a dying target does not cancel the AoE
        Vector3 center = target.transform.position;
        float halfDamage = 52f * (1f + 0.6f * source.elementalAffinity);
        DamageTag[] tags = WindshearTags;

        // Botanical Alchemy: setting Windshear off pays the Wind plant, and so does every damage it
        // deals, both hits on every insect (Tommy, 2026-11-23), at most the cap for this one blast
        int applied = 0, blastPayouts = 0;
        MulchManager.PayReaction(source, center, ref applied);

        yield return FirstBurstWait;
        DealWindshearAoe(center, halfDamage, ElementalType.Wind, source, tags, ref blastPayouts);
        yield return SecondBurstWait;
        DealWindshearAoe(center, halfDamage, primerElement, source, tags, ref blastPayouts);

        OnWindshearTriggered?.Invoke(center, source);
    }

    // made once and shared by every windshear: nothing writes into them
    private static readonly DamageTag[] WindshearTags = { DamageTag.ElementalDebuff };
    private static readonly WaitForSeconds FirstBurstWait = new WaitForSeconds(0.1f);
    private static readonly WaitForSeconds SecondBurstWait = new WaitForSeconds(0.05f);

    // deals one burst of the reaction damage to every insect within WindshearRadius of the center
    private static void DealWindshearAoe(Vector3 center, float damage, ElementalType element, Entity source, DamageTag[] tags, ref int payouts)
    {
        // a snapshot, since the damage can kill insects or set off another windshear. pooled, so
        // a burst inside this one's damage gets a list of its own
        System.Collections.Generic.List<Insect> insects = UnityEngine.Pool.ListPool<Insect>.Get();
        insects.AddRange(Insect.allInsects);
        try
        {
            const float radiusSqr = WindshearRadius * WindshearRadius;
            for (int i = 0; i < insects.Count; i++)
            {
                Insect insect = insects[i];
                if (insect == null || !insect.IsAlive) continue;
                if ((insect.transform.position - center).sqrMagnitude > radiusSqr) continue;
                MulchManager.PayReaction(source, insect.transform.position, ref payouts);
                insect.Damage(damage, DamageType.Magic, element, source, source?.ElementalReactionCanCrit ?? false, tags);
            }
        }
        finally
        {
            UnityEngine.Pool.ListPool<Insect>.Release(insects);
        }
    }

}
