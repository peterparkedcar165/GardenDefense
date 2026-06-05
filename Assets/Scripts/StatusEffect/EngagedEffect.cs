// applied to an enemy insect by a friendly combatant (minion or hypnotized insect) that is
// fighting it. it is a TauntEffect, so the enemy's existing target logic forces it to attack
// the engager and stop advancing, until the effect expires (the friendly stops refreshing it)
public class EngagedEffect : TauntEffect
{
    public EngagedEffect(Entity target, float duration, int level, Entity source, IAttackable taunter)
        : base(target, duration, level, source, taunter)
    {
        effectType = Type.neutral;   // override Taunt's negative so cleanses/debuff immunity can't break the block
    }

    public override string GetName() => "Engaged";
    public override string GetDescription() => "Locked in combat, unable to advance.";
}
