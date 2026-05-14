using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColdEffect : ElementalDebuff
{
    public ColdEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {

    }

    public override string GetName() => "<color=#00BFFF>Cold</color>";
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

        if (insect.HasEffect<BlazeEffect>())
        {
            insect.RemoveEffect<ColdEffect>();
            insect.RemoveEffect<BlazeEffect>();
            insect.ApplyEffect(new FractureEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<SproutEffect>())
        {
            insect.RemoveEffect<ColdEffect>();
            insect.RemoveEffect<SproutEffect>();
            insect.ApplyEffect(new BrittleEffect(insect, 8f, 1, source));
        }
        else if (insect.HasEffect<TaintedEffect>())
        {
            insect.RemoveEffect<ColdEffect>();
            insect.RemoveEffect<TaintedEffect>();
            insect.ApplyEffect(new FrostbiteEffect(insect, 6f, 1, source));
        }
        else if (insect.HasEffect<WetEffect>())
        {
            if (insect.freezeInternalCooldown <= 0)
            {
                insect.RemoveEffect<ColdEffect>();
                insect.RemoveEffect<WetEffect>();
                insect.freezeInternalCooldown = 5f;
                insect.ApplyEffect(new FreezeEffect(insect, 2f, 1, source));
            }
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
                nearby.ApplyEffect(new ColdEffect(nearby, 6f, 1, gustSource));
                nearby.Damage(windDamage, DamageType.Magic, ElementalType.Wind, gustSource, false, tags);
            }
        }
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
