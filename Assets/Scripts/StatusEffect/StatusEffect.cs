using UnityEngine;

public class StatusEffect
{
    public float duration;
    public enum Type
    {
        positive,
        neutral,
        negative,
        primer
    }

    public enum EffectTag
    {
        ElementalReaction,
        DoT,
        CrowdControl,
        Slow,
        Regenerative
    }
    public Type effectType;
    public ElementalType elementalType = ElementalType.Neutral;
    public bool visible = true;
    public int level;
    public Entity target, source;

    public EffectTag[] tags = new EffectTag[0];
    public bool sourceStackable = false;

    // a duration handed over exactly as it is (Plague Bearer's spread): Entity.ApplyEffect leaves it
    // alone instead of scaling it by the source's and target's duration stats again
    public bool durationIsFinal;

    // an elemental reaction's effect (Burn, Poisoned, Frostbite, Corrosion, Freeze, Steam, Sludge,
    // Brittle, Fracture, Germinate): applying it pays Botanical Alchemy (Entity.ApplyEffect)
    public virtual bool IsReaction => false;
    // the Botanical Alchemy payouts this reaction has made on its target, at most
    // BotanicalAlchemyMulchData.MaxEventsPerReaction; back to 0 whenever the reaction is refreshed
    public int alchemyPayouts;


    public StatusEffect(Entity target, float duration, int level, Entity source)
    {
        this.target = target;
        this.duration = duration;
        this.level = level;
        this.source = source;
    }

    public virtual void OnApply() {}
    public virtual void OnTick(float deltaTime) {}
    public virtual void OnExpire() {}
    // it ran out on its own (Entity.TickEffects), right before OnExpire. never called when it is
    // cleansed, used up, replaced or removed early, or when its target dies
    public virtual void OnNaturalExpire() {}
    public virtual void OnTargetDied() {}
    public virtual void OnReapply(StatusEffect previous) {}
    public virtual void OnDamageReceived(ElementalType elementalType, Entity damageSource, DamageTag[] damageTags = null) {}
    public virtual bool TryBlockNegativeEffect(StatusEffect incoming) => false;

    public bool IsExpired()
    {
        return duration <= 0;
    }

    // convenience for any effect's OnTick (or OnApply) that heals its target - automatically
    // attaches HealTag.Regeneration if this effect itself carries EffectTag.Regenerative, so a
    // mulch/stat that cares about "regenerative healing" specifically (e.g. Rejuvenator) doesn't
    // need every such effect to remember to flag it by hand. use this instead of calling
    // target.Heal() directly from any effect whose own tags include Regenerative
    protected void HealTarget(float amount)
    {
        bool isRegen = false;
        if (tags != null)
            foreach (EffectTag t in tags)
                if (t == EffectTag.Regenerative) { isRegen = true; break; }

        target.Heal(amount, source, isRegen ? new HealTag[] { HealTag.Regeneration } : null);
    }


    public virtual string GetDescription()
    {
        return "";
    }

    public virtual string GetGeneralDescription()
    {
        return "";
    }

    public virtual string GetName()
    {
        return "";
    }

}
