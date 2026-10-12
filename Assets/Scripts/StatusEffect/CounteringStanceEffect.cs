// Cactus's Skill: Countering Stance. While active, grants flat Physical Resistance and reflects
// a % of any melee hit's RAW pre-mitigation damage (EntityEventData.amount, not the post-armor
// .damage) back at the attacker - see Cactus.HandleIncomingHit for the actual reflection.
// lives entirely on its own timer: the self-shield cast alongside it (CactusShieldEffect) is a
// separate effect with its own lifetime, so the shield breaking early doesn't cut this short
public class CounteringStanceEffect : StatusEffect
{
    public const float PhysicalResistanceBonus = 0.15f;

    public CounteringStanceEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Poison;
    }

    public override string GetName() => "<color=purple><b>Countering Stance</b></color>";
    public override string GetDescription() =>
        $"Gains <color=green><b>{PhysicalResistanceBonus * 100f:F0}%</b></color> Physical Resistance. Taking melee damage returns a % of the pre-mitigation damage back to the attacker.";
}
