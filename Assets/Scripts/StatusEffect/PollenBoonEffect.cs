// Dandelion's Path2 max bonus: every allied plant within her attack radius (never herself)
// gets increased Skill Damage for as long as they stay in range - see PlantAuraBuffEffect for
// the continuous radius-based reapplication/expiry convention this follows
public class PollenBoonEffect : PlantAuraBuffEffect
{
    private readonly float skillDamageBonus;

    public PollenBoonEffect(Entity target, int level, Plant source, float range, float skillDamageBonus)
        : base(target, level, source, range)
    {
        this.skillDamageBonus = skillDamageBonus;
        effectType = Type.positive;
        elementalType = ElementalType.Wind;
        sourceStackable = true;
    }

    public override void OnApply()  => target.skillDamageAdder += skillDamageBonus;
    public override void OnExpire() => target.skillDamageAdder -= skillDamageBonus;

    public override string GetName() => "<color=#B2EBF2>Pollen Boon</color>";
    public override string GetDescription() =>
        $"<color=green><b>Skill Damage</b></color> increased by <color=green><b>{skillDamageBonus * 100f:F0}%</b></color>.";
}
