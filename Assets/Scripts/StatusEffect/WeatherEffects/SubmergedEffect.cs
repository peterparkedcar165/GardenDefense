// applied to every plant while the level's weather includes Underwater. non-Water plants get
// their Projectile Speed halved (Water plants swim fine); everyone's Fire damage is dampened
// and Water damage is boosted while submerged. Air depletion itself is handled separately in
// Plant.cs's own per-frame update (mirroring how Hot/Cold temperature drift works), gated on
// this effect being present — this effect just marks "this plant is currently submerged."
public class SubmergedEffect : StatusEffect
{
    private const float FireDamagePenalty  = 0.5f;
    private const float WaterDamageBonus   = 0.2f;
    private const float ProjectileSpeedPenalty = 0.5f;

    private bool _appliedProjectilePenalty;

    public SubmergedEffect(Entity target, Entity source, int intensity)
        : base(target, float.MaxValue, intensity, source)
    {
        effectType = Type.neutral;
        elementalType = ElementalType.Water;
    }

    public override void OnApply()
    {
        target.fireDamageAdder  -= FireDamagePenalty;
        target.waterDamageAdder += WaterDamageBonus;

        if (target is Plant plant && plant.elementalType != ElementalType.Water)
        {
            target.projectileSpeedMultiplier -= ProjectileSpeedPenalty;
            _appliedProjectilePenalty = true;
        }
    }

    public override void OnExpire()
    {
        target.fireDamageAdder  += FireDamagePenalty;
        target.waterDamageAdder -= WaterDamageBonus;

        if (_appliedProjectilePenalty)
            target.projectileSpeedMultiplier += ProjectileSpeedPenalty;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=#1B6CA8><b>Submerged</b></color>";
    public override string GetDescription()
    {
        string s = $"Reduce <color=orange><b>Fire Damage</b></color> by <color=red><b>{FireDamagePenalty * 100f:F0}%</b></color> and increase <color=#4FC3F7><b>Water Damage</b></color> by <color=green><b>{WaterDamageBonus * 100f:F0}%</b></color>.";
        if (_appliedProjectilePenalty)
            s += $"\nReduce <color=green><b>Projectile Speed</b></color> by <color=red><b>{ProjectileSpeedPenalty * 100f:F0}%</b></color>.";
        return s;
    }
}
