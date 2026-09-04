using UnityEngine;

public class FreezeEffect : HardCrowdControl, IElementalAffinityEffect
{
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float physicalResistBonus;
    private float physicalArmorBonus;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    // once Freeze lands on a target, Freeze specifically can't land again for duration + 3s
    public override float InternalCooldownAfterExpiry => 3f;

    public FreezeEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        // being frozen solid blocks physical hits, but higher elemental affinity from the
        // source chips away at how much protection the ice actually grants the target:
        // every 100% affinity knocks 20 percentage points off the bonus, down to a floor of 0%
        physicalResistBonus = Mathf.Max(0f, 0.5f - 0.2f * source.elementalAffinity);
        physicalArmorBonus = 100f * physicalResistBonus / (1f - Mathf.Min(physicalResistBonus, 0.99f));

        effectType = Type.negative;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Freeze</color>";
    public override string GetDescription() =>
        $"Target is completely frozen in place for <color=green><b>{duration:F1}s</b></color>. " +
        $"Physical Resistance increased by <color=green><b>{physicalResistBonus * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        base.OnApply(); // sets the internal cooldown via InternalCooldownAfterExpiry
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Freeze", new Color(0f, 1f, 1f));

        Insect insect = (Insect)target;
        insect.armorAdder += physicalArmorBonus;
    }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.armorAdder -= physicalArmorBonus;
    }
}
