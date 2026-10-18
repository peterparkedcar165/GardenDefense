public class FruitFlySwarmEffect : StatusEffect
{
    public const float speedBonusPerLevel = 0.1f;

    public FruitFlySwarmEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        if (target is Insect insect)
            insect.movementSpeedAdder += level * speedBonusPerLevel;
    }

    public override void OnExpire()
    {
        if (target is Insect insect)
            insect.movementSpeedAdder -= level * speedBonusPerLevel;
    }

    // called each scan; adjusts the stat delta and refreshes duration
    public void Refresh(int newLevel, Entity newSource)
    {
        int delta = newLevel - level;
        level = newLevel;
        source = newSource;
        duration = 0.35f;
        if (target is Insect insect)
            insect.movementSpeedAdder += delta * speedBonusPerLevel;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#C9A0DC>Fruit Fly Swarm</color>";
    public override string GetDescription() =>
        $"<b>Movement Speed</b> increased by <color=#C9A0DC><b>{level * speedBonusPerLevel:F1}</b></color> ({level} nearby fruit fl{(level == 1 ? "y" : "ies")}).";
}
