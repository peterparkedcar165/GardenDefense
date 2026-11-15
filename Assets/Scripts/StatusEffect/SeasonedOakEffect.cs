// seasoned oak mulch: shows an acorn knight's armor climb in his status effects. display only.
// SeasonedOakMulchEffect owns the armor itself and updates these numbers every frame, so a
// cleanse or anything else that strips this can't change his armor (the mulch puts it right
// back). neutral on purpose: it's a penalty that turns into a bonus, and neutral effects are
// never cleansed, blocked or rescaled the way buffs and debuffs are
public class SeasonedOakEffect : StatusEffect
{
    public float armor;
    public float armorPerStep;
    public float maxArmor;
    public float secondsToNextStep;

    public SeasonedOakEffect(Entity target) : base(target, float.MaxValue, 1, null)
    {
        effectType = Type.neutral;
        elementalType = ElementalType.Grass;
    }

    public override string GetName() => "<color=green>Seasoned Oak</color>";

    public override string GetDescription()
    {
        string value = armor < 0f ? $"<color=red><b>{armor:0.#}</b></color>" : $"<color=green><b>+{armor:0.#}</b></color>";
        string armorLabel = "<color=#00CED1><b>Armor</b></color>";
        if (armor >= maxArmor)
            return $"{value} {armorLabel}, fully seasoned.";
        return $"{value} {armorLabel}. Gains <color=green><b>+{armorPerStep:0.#}</b></color> {armorLabel} in " +
            $"<color=green><b>{secondsToNextStep:0.0}s</b></color>, up to <color=green><b>+{maxArmor:0.#}</b></color>.";
    }
}
