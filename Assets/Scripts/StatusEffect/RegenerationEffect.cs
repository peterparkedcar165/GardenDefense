using UnityEngine;

// base class for healing-over-time effects. subclass this to create specialised regen variants
public class RegenerationEffect : StatusEffect
{
    protected float tickTimer = 0f;
    protected float tickInterval = 1f;
    protected float healingPerSecond;

    public RegenerationEffect(Entity target, float duration, int level, Entity source, float healingPerSecond, float tickInterval = 1f)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        tags = new EffectTag[] { EffectTag.Regenerative };
        this.healingPerSecond = healingPerSecond;
        this.tickInterval     = tickInterval;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(-0.4f, 0f, 0f), "Regen", new Color(0.4f, 1f, 0.4f));
    }

    public override void OnTick(float deltaTime)
    {
        tickTimer += deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            target.Heal(healingPerSecond, source);
        }
    }

    public override string GetName() => "<color=#88FF88>Regeneration</color>";
    public override string GetDescription() => $"Recovering <color=#88FF88><b>{healingPerSecond:F0}</b></color> health per second.";
}
