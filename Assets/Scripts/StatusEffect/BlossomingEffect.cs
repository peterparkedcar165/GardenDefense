using UnityEngine;

public class BlossomingEffect : StatusEffect
{
    private readonly float grassDamageBonus;
    private readonly float attackSpeedBonus;

    public const float GerminateRadiusMultiplier = 1.5f;

    // read by GerminateEffect on construction, so a Germinate this plant causes snapshots the
    // radius bonus at the moment it's applied - it keeps the bigger radius even if Blossoming
    // itself expires before that Germinate detonates
    public bool GrantsGerminateRadiusBonus { get; private set; }

    public BlossomingEffect(Entity target, float duration, int level, Entity source, float grassDamageBonus, float attackSpeedBonus)
        : base(target, duration, level, source)
    {
        this.grassDamageBonus = grassDamageBonus;
        this.attackSpeedBonus = attackSpeedBonus;
        effectType      = Type.positive;
        elementalType   = ElementalType.Grass;
        sourceStackable = true;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Blossoming", new Color(0.3f, 1f, 0.2f));
        target.grassDamageAdder += grassDamageBonus;
        target.attackSpeedMultiplier += attackSpeedBonus;

        GrantsGerminateRadiusBonus = source is Begonia beg && beg.path3Level >= Plant.absoluteLevelCap;
    }

    public override void OnExpire()
    {
        target.grassDamageAdder -= grassDamageBonus;
        target.attackSpeedMultiplier -= attackSpeedBonus;
    }

    public override void OnTick(float deltaTime) { }

    public override string GetName() => "<color=green>Blossoming</color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Grass Damage</b></color> by <color=green><b>{grassDamageBonus * 100f:F0}%</b></color>, and <color=green><b>Attack Speed</b></color> by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>.";
        if (GrantsGerminateRadiusBonus)
            desc += $" Also increases <color=green><b>Germinate</b></color> radius by <color=green><b>{(GerminateRadiusMultiplier - 1f) * 100f:F0}%</b></color>.";
        return desc;
    }
}
