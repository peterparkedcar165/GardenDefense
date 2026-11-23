using UnityEngine;

public class FrostbiteEffect : DoTEffect
{
    private static readonly DamageTag[] tickTags = { DamageTag.DoT, DamageTag.ElementalDebuff };

    public float healthPerSecond = 0.01f, flatPerSecond = 6f;
    private float baseMovementSlow = 0.10f;
    private float movementSlow;
    private float cachedelementalAffinity;
    private float cachedMaxHealth;

    public override bool IsReaction => true;

    public FrostbiteEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source, 1f)
    {
        cachedelementalAffinity = source?.elementalAffinity ?? 0f;
        movementSlow = baseMovementSlow * (1f + cachedelementalAffinity) + MulchManager.SlowBonusFrom(source);
        effectType = Type.negative;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Frostbite</color>";
    public override string GetDescription()
    {
        float hp = cachedMaxHealth > 0 ? cachedMaxHealth : (target?.maxHealth ?? 0f);
        float ep = cachedelementalAffinity;
        float total = (healthPerSecond * hp + flatPerSecond) * (1f + 0.33f * ep) * (1f + ticks.tickSpeedBonus);
        return $"Deal <color=#00BFFF><b>{total:F0}</b></color> <color=#00BFFF>Ice</color> Physical damage per second. " +
               $"Reduces Movement Speed by <color=green>{movementSlow * 100f:F0}%</color>.";
    }

    public override void OnApply()
    {
        base.OnApply();
        cachedMaxHealth = target.maxHealth;
        damagePerSecond = (healthPerSecond * cachedMaxHealth + flatPerSecond) * (1f + 0.33f * cachedelementalAffinity);

        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Frostbite", new Color(0f, 1f, 1f));

        if (target is Insect insect)
            insect.movementSpeedMultiplier -= movementSlow;
    }

    protected override void DealTick(float share)
    {
        float tick = damagePerSecond * ticks.baseInterval * share;
        if (source != null)
            target.Damage(tick, DamageType.Physical, ElementalType.Ice, source, source.DotCanCrit || source.ElementalReactionCanCrit, tickTags);
        else
            target.Damage(tick, DamageType.Physical, ElementalType.Ice, tickTags);
        MulchManager.ReactionEvent(this, source, target.transform.position);
    }

    public override void OnExpire()
    {
        if (target is Insect insect)
            insect.movementSpeedMultiplier += movementSlow;
    }
}
