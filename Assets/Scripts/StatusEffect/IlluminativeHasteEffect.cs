public class IlluminativeHasteEffect : StatusEffect
{
    private const float speedBonus = 0.4f;

    public IlluminativeHasteEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        if (target is Insect insect)
            insect.movementSpeedMultiplier += speedBonus;
    }

    public override void OnExpire()
    {
        if (target is Insect insect)
            insect.movementSpeedMultiplier -= speedBonus;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#378745>Illuminative Haste</color>";
    public override string GetDescription() => $"Movement Speed increased by {speedBonus * 100:F0}% while near a light source.";
}
