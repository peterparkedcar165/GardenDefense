// Monsoon (skill tree): refreshed every heal-tick on any plant Soothing Rain touches, same
// refresh-while-inside idiom as RainExposedEffect - expires shortly after the plant leaves
public class MonsoonSurgeEffect : StatusEffect
{
    private const float WaterDamageBonus = 0.12f;

    public MonsoonSurgeEffect(Entity target, Entity source) : base(target, 1f, 1, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Water;
    }

    public override void OnApply() => target.waterDamageAdder += WaterDamageBonus;
    public override void OnExpire() => target.waterDamageAdder -= WaterDamageBonus;

    public override string GetName() => "<color=#4FC3F7>Monsoon</color>";
    public override string GetDescription() =>
        $"Water Damage increased by <color=green><b>{WaterDamageBonus * 100f:F0}%</b></color>.";
}
