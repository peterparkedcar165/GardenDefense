using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GustEffect : ElementalDebuff
{
    public GustEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {

    }

    public override string GetName() => "<color=#E0E0E0>Gust</color>";
    public override string GetDescription() => "When another elemental primer lands, it is refreshed and spread to nearby insects. Gust is then consumed.";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<BlazeEffect>())
        {
            Entity primerSource = insect.GetEffect<BlazeEffect>().source;
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(SpreadAfterDelay(insect, source, n => n.ApplyEffect(new BlazeEffect(n, 6f, 1, primerSource))));
            return;
        }
        if (insect.HasEffect<ColdEffect>())
        {
            Entity primerSource = insect.GetEffect<ColdEffect>().source;
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(SpreadAfterDelay(insect, source, n => n.ApplyEffect(new ColdEffect(n, 6f, 1, primerSource))));
            return;
        }
        if (insect.HasEffect<WetEffect>())
        {
            Entity primerSource = insect.GetEffect<WetEffect>().source;
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(SpreadAfterDelay(insect, source, n => n.ApplyEffect(new WetEffect(n, 6f, 1, primerSource))));
            return;
        }
        if (insect.HasEffect<TaintedEffect>())
        {
            Entity primerSource = insect.GetEffect<TaintedEffect>().source;
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(SpreadAfterDelay(insect, source, n => n.ApplyEffect(new TaintedEffect(n, 6f, 1, primerSource))));
            return;
        }
        if (insect.HasEffect<SproutEffect>())
        {
            Entity primerSource = insect.GetEffect<SproutEffect>().source;
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(SpreadAfterDelay(insect, source, n => n.ApplyEffect(new SproutEffect(n, 6f, 1, primerSource))));
            return;
        }

        insect.RemoveEffect<GustEffect>();
    }

    private static IEnumerator SpreadAfterDelay(Insect origin, Entity gustSource, System.Action<Insect> apply)
    {
        yield return new WaitForSeconds(0.1f);
        float windDamage = 12f + (0.5f * gustSource.attackDamage);
        DamageTag[] tags = new DamageTag[] { DamageTag.AoE, DamageTag.ElementalDebuff };
        origin.Damage(windDamage, DamageType.Magic, ElementalType.Wind, gustSource, false, tags);
        foreach (Insect nearby in new List<Insect>(Insect.allInsects))
        {
            if (nearby == origin) continue;
            if (Vector3.Distance(origin.transform.position, nearby.transform.position) <= 1.5f)
            {
                apply(nearby);
                nearby.Damage(windDamage, DamageType.Magic, ElementalType.Wind, gustSource, false, tags);
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
