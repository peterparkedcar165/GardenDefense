public class SnowExposedEffect : StatusEffect
{
    private const float baseBonus     = 0.12f;
    private const float bonusPerLevel = 0.08f;

    private readonly float bonus;

    public SnowExposedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.neutral;
        elementalType = ElementalType.Ice;
        bonus = baseBonus + bonusPerLevel * (level - 1);
    }

    public override void OnApply()  => target.iceDamageAdder += bonus;
    public override void OnExpire() => target.iceDamageAdder -= bonus;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#00FFFF>[Weather]: Snow</color>";
    public override string GetDescription() =>
        $"Increase <color=#88DDFF><b>Ice Damage</b></color> by <color=#88DDFF><b>{bonus * 100f:F0}%</b></color>.";
}
