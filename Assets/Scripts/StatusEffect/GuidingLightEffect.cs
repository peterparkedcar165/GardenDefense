// Calendula's Guiding Light node: refreshed every 0.25s (see Calendula.Update) on any insect
// currently within her illumination radius, so it never lapses while they stay lit
public class GuidingLightEffect : StatusEffect
{
    private const float armorShred = 20f;

    public GuidingLightEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Fire;
    }

    public override void OnApply()
    {
        Insect insect = (Insect)target;
        insect.armorAdder -= armorShred;
        insect.magicArmorAdder -= armorShred;
    }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.armorAdder += armorShred;
        insect.magicArmorAdder += armorShred;
    }

    public override string GetName() => "<color=orange>Guiding Light</color>";
    public override string GetDescription() =>
        $"Armor and Magic Armor reduced by <color=red><b>{armorShred:F0}</b></color> while illuminated.";
}
