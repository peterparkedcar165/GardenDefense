public class ScoutsEncouragementEffect : StatusEffect
{
    private readonly float speedBonus;

    public ScoutsEncouragementEffect(Entity target, float duration, int level, Entity source, float speedBonus)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        this.speedBonus = speedBonus;
    }

    public override void OnApply()
    {
        if (target is Insect insect)
            insect.movementSpeedAdder += speedBonus;
    }

    public override void OnExpire()
    {
        if (target is Insect insect)
            insect.movementSpeedAdder -= speedBonus;
    }

    public override string GetName() => "<color=#8B4513>Scout's Encouragement</color>";
    public override string GetDescription() => $"<color=green><b>Movement Speed</b></color> increased by <color=green><b>{speedBonus:F1}</b></color> by a nearby Scout Ant.";
}
