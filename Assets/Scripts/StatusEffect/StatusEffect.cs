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
    public int level;
    public Entity target, source;

    public EffectTag[] tags = new EffectTag[0];


    public StatusEffect(Entity target, float duration, int level, Entity source)
    {
        this.target = target;
        this.duration = duration;
        this.level = level;
        this.source = source;
    }

    public virtual void OnApply() {} // nothing because will be implemented in specific effects
    public virtual void OnTick(float deltaTime) {} // nothing because will be implemented in specific effects
    public virtual void OnExpire() {} // same

    public bool IsExpired()
    {
        return duration <= 0;
    }

    public virtual void Tick(float deltaTime)
    {
        duration -= deltaTime;
        OnTick(deltaTime);
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

    public string GetEffectTagsString()
    {
        return string.Join(", ", tags);
    }

    public string GetEffectType()
    {
        return effectType.ToString();
    }
}
