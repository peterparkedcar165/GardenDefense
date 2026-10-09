// Dandelion's Wind Gust: any allied plant touched by the gust (never herself) gains this for
// a flat duration, boosting Skill Charge Rate - see Plant.skillChargeRate/skillChargeRateAdder
public class PollenHasteEffect : StatusEffect
{
    private readonly float bonus;

    public PollenHasteEffect(Entity target, float duration, Entity source, float bonus)
        : base(target, duration, 1, source)
    {
        effectType = Type.positive;
        elementalType = ElementalType.Wind;
        sourceStackable = true;
        this.bonus = bonus;
    }

    public override void OnApply()  => ((Plant)target).skillChargeRateAdder += bonus;
    public override void OnExpire() => ((Plant)target).skillChargeRateAdder -= bonus;

    public override string GetName() => "<color=#B2EBF2>Pollen Haste</color>";
    public override string GetDescription() =>
        $"<color=green><b>Skill Charge Rate</b></color> increased by <color=green><b>{bonus * 100f:F0}%</b></color>.";
}
