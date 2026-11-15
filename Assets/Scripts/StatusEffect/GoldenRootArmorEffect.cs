// golden root mulch: flat armor on a plant a rhodiola just healed. same type replaces itself in
// Entity.ApplyEffect, so a fresh heal refreshes the duration instead of stacking the armor
public class GoldenRootArmorEffect : StatusEffect
{
    private readonly float armor;

    public GoldenRootArmorEffect(Entity target, float duration, int level, Entity source, float armor)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Grass;
        this.armor = armor;
    }

    public override string GetName() => "<color=#FFD700>Golden Root</color>";
    public override string GetDescription() =>
        $"Increase <color=#00CED1><b>Armor</b></color> by <color=green><b>{armor:0.#}</b></color>.";

    public override void OnApply() => target.armorAdder += armor;
    public override void OnExpire() => target.armorAdder -= armor;
}
