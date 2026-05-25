using System.Collections;
using UnityEngine;

public class GustEffect : ElementalDebuff
{
    public GustEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {

    }

    public override string GetName() => "<color=#E0E0E0>Gust</color>";
    public override string GetDescription()
    {
        float halfDamage = 32f * (1f + 1.5f * source.elementalPower);
        return $"Reacts with an existing primer, dealing <color=#E0E0E0><b>{halfDamage:F0}</b></color> Wind + <color=#E0E0E0><b>{halfDamage:F0}</b></color> elemental Magic damage. (32 × (1 + 1.5× <color=#FFD700>{source.elementalPower * 100:F0}% Elemental Power</color>))";
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
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Windshear", new Color(0.85f, 1f, 0.85f));
        yield return new WaitForSeconds(0.1f);
        float halfDamage = 32f * (1f + 1.5f*(source.elementalPower));
        DamageTag[] tags = new DamageTag[] { DamageTag.ElementalDebuff };
        target.Damage(halfDamage, DamageType.Magic, ElementalType.Wind, source, false, tags);
        yield return new WaitForSeconds(0.05f);
        target.Damage(halfDamage, DamageType.Magic, primerElement, source, false, tags);
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
