public class RainExposedEffect : StatusEffect
{
    private const float baseBonus     = 0.24f;
    private const float bonusPerLevel = 0.12f;

    private readonly float bonus;

    public RainExposedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.neutral;
        bonus = baseBonus + bonusPerLevel * (intensity - 1);
    }

    public override void OnApply()  => target.waterDamageAdder += bonus;
    public override void OnExpire() => target.waterDamageAdder -= bonus;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#4488FF>Exposed: Rain</color>";
    public override string GetDescription() =>
        $"Rainy weather (intensity {level}) empowers Water damage by <color=#4488FF><b>{bonus * 100f:F0}%</b></color>.";
}
