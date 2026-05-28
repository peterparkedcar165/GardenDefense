public class SnowExposedEffect : StatusEffect
{
    private const float baseBonus     = 0.24f;
    private const float bonusPerLevel = 0.12f;

    private readonly float bonus;

    public SnowExposedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.neutral;
        bonus = baseBonus + bonusPerLevel * (level - 1);
    }

    public override void OnApply()  => target.iceDamageAdder += bonus;
    public override void OnExpire() => target.iceDamageAdder -= bonus;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#88DDFF>Exposed: Snow</color>";
    public override string GetDescription() =>
        $"Increase <color=#88DDFF>Ice</color> Damage by <color=#88DDFF><b>{bonus * 100f:F0}%</b></color>.";
}
