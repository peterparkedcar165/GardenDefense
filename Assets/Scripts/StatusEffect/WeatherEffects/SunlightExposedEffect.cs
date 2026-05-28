public class SunlightExposedEffect : StatusEffect
{
    private const float baseBonus    = 0.24f;
    private const float bonusPerLevel = 0.12f;

    private readonly float bonus;

    public SunlightExposedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.neutral;
        // intensity 1 = 24%, intensity 2 = 36%, intensity 3 = 48%, etc.
        bonus = baseBonus + bonusPerLevel * (intensity - 1);
    }

    public override void OnApply()   => target.fireDamageAdder  += bonus;
    public override void OnExpire()  => target.fireDamageAdder  -= bonus;
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=orange>Exposed: Sunlight</color>";
    public override string GetDescription() =>
        $"Sunny weather (intensity {level}) empowers Fire damage by <color=orange><b>{bonus * 100f:F0}%</b></color>.";
}
