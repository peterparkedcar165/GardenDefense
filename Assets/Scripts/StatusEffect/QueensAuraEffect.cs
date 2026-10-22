// Queen Ant's own aura buff (see QueenAnt.ApplyQueensAura) - same regen-over-time base as any other
// RegenerationEffect, plus a flat Armor/Magic Armor grant on top while it's up
public class QueensAuraEffect : RegenerationEffect
{
    private readonly float armorBonus;

    public QueensAuraEffect(Entity target, float duration, int level, Entity source, float healingPerSecond, float armorBonus, float tickInterval = 1f)
        : base(target, duration, level, source, healingPerSecond, tickInterval)
    {
        this.armorBonus = armorBonus;
        // only one Queen's Aura at a time, regardless of how many Queen Ants are in range -
        // overrides RegenerationEffect's own default of true (which lets independent sources,
        // e.g. two different healer plants, each keep their own stacked instance)
        sourceStackable = false;
    }

    // deliberately skips base.OnApply() - that spawns RegenerationEffect's own "Regeneration"
    // status indicator, which would otherwise pop up constantly since this effect keeps getting
    // reapplied every scan tick to every ant in range
    public override void OnApply()
    {
        if (target is Insect insect)
        {
            insect.armorAdder += armorBonus;
            insect.magicArmorAdder += armorBonus;
        }
    }

    public override void OnExpire()
    {
        if (target is Insect insect)
        {
            insect.armorAdder -= armorBonus;
            insect.magicArmorAdder -= armorBonus;
        }
    }

    public override string GetName() => "<color=#FFD700>Queen's Aura</color>";
    public override string GetDescription() =>
        $"Recovering <color=#88FF88><b>{healingPerSecond:F0}</b></color> health per second, and <color=#00CED1><b>+{armorBonus:F0} Armor</b></color> / <color=#00CED1><b>Magic Armor</b></color>, from a nearby Queen Ant.";
}
