public class SunlightExposedEffect : StatusEffect
{
    private const float baseBonus    = 0.24f;
    private const float bonusPerLevel = 0.12f;

    private readonly float bonus;

    public SunlightExposedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.neutral;
        elementalType = ElementalType.Fire;
        bonus = baseBonus + bonusPerLevel * (level - 1);
    }

    public override void OnApply()   => target.fireDamageAdder  += bonus;
    public override void OnExpire()  => target.fireDamageAdder  -= bonus;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=orange>[Weather]: Sunny</color>";
    public override string GetDescription() =>
        $"Increase <color=orange><b>Fire Damage</b></color> by <color=orange><b>{bonus * 100f:F0}%</b></color>.";
}
