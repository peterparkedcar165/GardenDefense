using UnityEngine;

// applied when Windshear is consumed by a non-Wind elemental hit: reduces that element's resistance,
// scaled by the elemental affinity of whichever plant originally applied the Windshear primer.
// split into one subclass per element so a target can carry multiple Windsheared debuffs at once
public abstract class WindshearedEffect : StatusEffect, IElementalAffinityEffect
{
    protected readonly float shred;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    protected WindshearedEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        shred = 0.15f * (1f + (source?.elementalAffinity ?? 0f));
        effectType = Type.negative;
        elementalType = ElementalType.Wind;
    }

    protected abstract ElementalType ShredElement { get; }
    protected abstract void Adjust(float amount);

    public override string GetName() => "<color=#B2EBF2>Windsheared</color>";
    public override string GetDescription() =>
        $"Reduce {PlantData.ElementalTag(ShredElement)} <b>Resistance</b> by <color=red><b>{shred * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Windsheared", ElementColor);
        Adjust(-shred);
    }

    public override void OnExpire() => Adjust(shred);

    public override void OnTick(float deltaTime) { }

    // matches the indicator color each element's own proc effect uses (Burn, Soaked, Seeded, etc.)
    private Color ElementColor => ShredElement switch
    {
        ElementalType.Fire   => new Color(1f, 0.4f, 0f),
        ElementalType.Water  => new Color(0.31f, 0.76f, 0.97f),
        ElementalType.Grass  => new Color(0.3f, 0.7f, 0.3f),
        ElementalType.Poison => new Color(0.6f, 0.1f, 0.8f),
        ElementalType.Ice    => new Color(0f, 1f, 1f),
        ElementalType.Ground => new Color(0.47f, 0.22f, 0.12f),
        _                    => new Color(0.7f, 0.95f, 0.95f)
    };
}
