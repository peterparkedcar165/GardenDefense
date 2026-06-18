using UnityEngine;

public class RejuvenatingSeedEffect : StatusEffect
{
    private readonly Rhodiola _rhodiola;
    private readonly float  _fullDuration;
    private const float BurstRadius = 2.5f;

    public RejuvenatingSeedEffect(Entity target, float duration, Entity source)
        : base(target, duration, 1, source)
    {
        _rhodiola       = source as Rhodiola;
        _fullDuration   = duration;
        effectType      = Type.negative;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        Entity.OnEntityKilled += OnAnyEntityKilled;
    }

    public override void OnExpire()
    {
        Entity.OnEntityKilled -= OnAnyEntityKilled;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnDamageReceived(ElementalType elementalType, Entity damageSource, DamageTag[] damageTags = null)
    {
        if (_rhodiola != null && _rhodiola.IsPath2Maxed && elementalType == ElementalType.Water)
        {
            duration = _fullDuration;
            return;
        }

        if (!(damageSource is Plant plant)) return;
        if (damageTags == null || !System.Array.Exists(damageTags, t => t == DamageTag.Attack)) return;

        float healPerTick = _rhodiola != null ? _rhodiola.BurgeonHealPerTick   : 2f;
        float dur         = _rhodiola != null ? _rhodiola.BurgeonDuration      : 4f;
        float interval    = _rhodiola != null ? _rhodiola.BurgeonTickInterval  : 0.5f;
        plant.ApplyEffect(new RejuvenatingBurgeonEffect(plant, dur, 1, _rhodiola, healPerTick, interval));
    }

    private void OnAnyEntityKilled(EntityEventData data)
    {
        if (data.target != target) return;
        if (_rhodiola == null || !_rhodiola.IsPath1Maxed) return;

        Vector3 origin = target.transform.position;
        foreach (Insect insect in new System.Collections.Generic.List<Insect>(Insect.allInsects))
        {
            if (insect == null || !insect.IsAlive || insect == target) continue;
            if (Vector3.Distance(origin, insect.transform.position) > BurstRadius) continue;
            insect.ApplyEffect(new RejuvenatingSeedEffect(insect, _fullDuration, _rhodiola));
        }
    }

    public override string GetName()        => "<color=green><b>Rejuvenating Seed</b></color>";
    public override string GetDescription() => "When attacked by a plant, the attacker is granted <color=green><b>Rejuvenating Burgeon</b></color>.";
}
