public class FruitFlySwarmEffect : StatusEffect
{
    public const float resistBonusPerLevel = 12f;

    public FruitFlySwarmEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.armorAdder += level * resistBonusPerLevel;
        target.magicArmorAdder += level * resistBonusPerLevel;
    }

    public override void OnExpire()
    {
        target.armorAdder -= level * resistBonusPerLevel;
        target.magicArmorAdder -= level * resistBonusPerLevel;
    }

    // called each scan; adjusts the stat delta and refreshes duration
    public void Refresh(int newLevel, Entity newSource)
    {
        int delta = newLevel - level;
        level = newLevel;
        source = newSource;
        duration = 0.35f;
        target.armorAdder += delta * resistBonusPerLevel;
        target.magicArmorAdder += delta * resistBonusPerLevel;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#C9A0DC>Fruit Fly Swarm</color>";
    public override string GetDescription() =>
        $"<b>Armor</b> and <b>Magic Resist</b> increased by <color=#C9A0DC><b>{level * resistBonusPerLevel:F0}</b></color> ({level} nearby fruit fl{(level == 1 ? "y" : "ies")}).";
}
