using UnityEngine;

// Airborne (HardCrowdControl): the insect floats helplessly and takes extra crit chance.
// the updraft field keeps re-lifting and refreshing it; it only clears once the insect lands
public class LevitatingEffect : Airborne
{
    private readonly float critBonus;
    private Insect insect;
    private const float CritDamageBonus = 0.25f;
    private bool _increasesCritDamage;

    public LevitatingEffect(Entity target, float duration, int level, Entity source, float critBonus)
        : base(target, duration, level, source)
    {
        this.critBonus = critBonus;
        effectType = Type.negative;
        elementalType = ElementalType.Wind;
    }

    public override void OnApply()
    {
        insect = target as Insect;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Levitating", new Color(0.7f, 0.92f, 1f));
        target.bonusCritChanceReceivedAdder += critBonus;
        _increasesCritDamage = source is MorningGlory mg && mg.IsPath3Maxed;
        if (_increasesCritDamage) target.bonusCritDamageReceivedAdder += CritDamageBonus;
        // credit the lifter for the fall damage the insect takes when it drops back down
        if (insect != null) insect.fallDamageSource = source;
    }

    public override void OnExpire()
    {
        target.bonusCritChanceReceivedAdder -= critBonus;
        if (_increasesCritDamage) target.bonusCritDamageReceivedAdder -= CritDamageBonus;
    }

    public override void OnTick(float deltaTime)
    {
        // persists while airborne; removes only once the insect is back on the ground
        if (insect != null && insect.isOnGround && insect.verticalVelocity >= 0f)
            duration = 0f;
    }

    public override string GetName() => "<color=#B2EBF2>Levitating</color>";
    public override string GetDescription()
    {
        string desc = $"Suspended helplessly in the air. Takes <color=#FFD700><b>+{critBonus * 100f:F0}%</b></color> <color=#FFD700><b>Critical Chance</b></color> from all incoming damage";
        desc += _increasesCritDamage
            ? $", and <color=#FFD700><b>+{CritDamageBonus * 100f:F0}%</b></color> <color=#FFD700><b>Critical Damage</b></color>."
            : ".";
        return desc;
    }
}
