public class VerdantEmpowermentEffect : StatusEffect
{
    private const float damageBonus = 0.15f;
    private const float speedBonus  = 0.15f;

    public VerdantEmpowermentEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        target.attackDamageTotalMultiplier += damageBonus;
        target.attackSpeedTotalMultiplier  += speedBonus;
    }

    public override void OnExpire()
    {
        target.attackDamageTotalMultiplier -= damageBonus;
        target.attackSpeedTotalMultiplier  -= speedBonus;
    }

    public override string GetName() => "<color=green><b>Verdant Empowerment</b></color>";
    public override string GetDescription() =>
        $"Freshly revived, gaining <color=green><b>+{damageBonus * 100f:F0}%</b></color> Attack Damage and <color=green><b>+{speedBonus * 100f:F0}%</b></color> Attack Speed.";
}
