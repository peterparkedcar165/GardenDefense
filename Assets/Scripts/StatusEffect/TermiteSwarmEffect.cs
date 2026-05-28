public class TermiteSwarmEffect : StatusEffect
{
    private const float attackBonusPerLevel = 3f;

    public TermiteSwarmEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.attackDamageAdder += level * attackBonusPerLevel;
    }

    public override void OnExpire()
    {
        target.attackDamageAdder -= level * attackBonusPerLevel;
    }

    // called each scan; adjusts the stat delta and refreshes duration
    public void Refresh(int newLevel, Entity newSource)
    {
        int delta = newLevel - level;
        level = newLevel;
        source = newSource;
        duration = 0.35f;
        target.attackDamageAdder += delta * attackBonusPerLevel;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#DB5B00>Termite Swarm</color>";
    public override string GetDescription() =>
        $"Attack Damage increased by <color=#DB5B00><b>{level * attackBonusPerLevel:F0}</b></color> ({level} nearby termite{(level == 1 ? "" : "s")}).";
}
