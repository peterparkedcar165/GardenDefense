using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlazeEffect : ElementalDebuff
{
    public BlazeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {

    }

    public override string GetName() => "<color=#FF4500>Blaze</color>";
    public override string GetDescription() => "Used as a primer to react with other elements";

    public override void OnApply()
    {
        Insect insect = (Insect)target;

        if (insect.HasEffect<GustEffect>())
        {
            Entity gustSource = insect.GetEffect<GustEffect>().source;
            insect.RemoveEffect<GustEffect>();
            insect.StartCoroutine(SpreadAfterDelay(insect, gustSource));
            return;
        }

        if (insect.HasEffect<ColdEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<ColdEffect>();
            insect.ApplyEffect(new FractureEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<SproutEffect>();
            insect.ApplyEffect(new BurnEffect(insect, 5f, 1, source));
        }
        else if (insect.HasEffect<TaintedEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<TaintedEffect>();
            insect.ApplyEffect(new VulnerableEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<WetEffect>())
        {
            insect.RemoveEffect<BlazeEffect>();
            insect.RemoveEffect<WetEffect>();
            insect.ApplyEffect(new BoilEffect(insect, 8f, 1, source));
        }
    }

    private static IEnumerator SpreadAfterDelay(Insect origin, Entity gustSource)
    {
        yield return new WaitForSeconds(0.1f);
        float windDamage = 24f * (1 + gustSource.elementalPower);
        DamageTag[] tags = new DamageTag[] { DamageTag.AoE, DamageTag.ElementalDebuff };
        origin.Damage(windDamage, DamageType.Magic, ElementalType.Wind, gustSource, false, tags);
        foreach (Insect nearby in new List<Insect>(Insect.allInsects))
        {
            if (nearby == origin) continue;
            if (Vector3.Distance(origin.transform.position, nearby.transform.position) <= 2f)
            {
                nearby.ApplyEffect(new BlazeEffect(nearby, 6f, 1, gustSource));
                nearby.Damage(windDamage, DamageType.Magic, ElementalType.Wind, gustSource, false, tags);
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
