public class SlowingAllergenEffect : StatusEffect
{
    private const float moveSlow = 0.27f;
    private const float windResistReduction = 0.23f;

    public SlowingAllergenEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Wind;
    }

    public override void OnApply()
    {
        if (target is not Insect insect) return;
        insect.movementSpeedMultiplier -= moveSlow;
        insect.windResistanceAdder -= windResistReduction;
    }

    public override void OnExpire()
    {
        if (target is not Insect insect) return;
        insect.movementSpeedMultiplier += moveSlow;
        insect.windResistanceAdder += windResistReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#B2EBF2><b>Slowing Allergen</b></color>";
    public override string GetDescription() =>
        $"<color=green>Movement Speed</color> reduced by <color=green><b>{moveSlow * 100f:F0}%</b></color>. " +
        $"<color=#E0E0E0>Wind Resistance</color> reduced by <color=#E0E0E0><b>{windResistReduction * 100f:F0}%</b></color>.";
}
