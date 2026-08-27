public class CalendulasLightEffect : PlantAuraBuffEffect
{
    private readonly float attackSpeedBonus;

    public CalendulasLightEffect(Entity target, int level, Plant source, float range, float attackSpeedBonus)
        : base(target, level, source, range)
    {
        this.attackSpeedBonus = attackSpeedBonus;
        effectType      = Type.positive;
        elementalType   = ElementalType.Fire;
        sourceStackable = true;
    }

    public override void OnApply()  => target.attackSpeedMultiplier += attackSpeedBonus;
    public override void OnExpire() => target.attackSpeedMultiplier -= attackSpeedBonus;

    public override string GetName() => "<color=orange>Calendula's Light</color>";
    public override string GetDescription() =>
        $"Increase <color=green><b>Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>.";
}
