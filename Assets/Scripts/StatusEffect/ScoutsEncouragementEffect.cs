public class ScoutsEncouragementEffect : StatusEffect
{
    private const float speedBonus = 0.15f;

    public ScoutsEncouragementEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        if (target is Insect insect)
            insect.movementSpeedMultiplier += speedBonus;
    }

    public override void OnExpire()
    {
        if (target is Insect insect)
            insect.movementSpeedMultiplier -= speedBonus;
    }

    public override string GetName() => "<color=#8B4513>Scout's Encouragement</color>";
    public override string GetDescription() => $"Movement Speed increased by {speedBonus * 100:F0}% by a nearby Scout Ant.";
}
