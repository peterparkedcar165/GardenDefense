using UnityEngine;

// ground elemental effect proc: reduces physical resistance, scaled by the source's elemental affinity
public class VulnerableEffect : StatusEffect, IElementalAffinityEffect
{
    private float shred;
    private float armorReduction;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    public VulnerableEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        shred = 0.2f * (1f + source.elementalAffinity);
        armorReduction = 100f * shred / (1f - Mathf.Min(shred, 0.99f));
        effectType = Type.negative;
        elementalType = ElementalType.Ground;
    }

    public override string GetName() => "<color=#79391F>Vulnerable</color>";
    public override string GetDescription() =>
        $"Reduce <color=#00CED1><b>Physical Resistance</b></color> by <color=red><b>{shred * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Vulnerable", new Color(0.47f, 0.22f, 0.12f));

        Insect insect = (Insect)target;
        insect.armorAdder -= armorReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.armorAdder += armorReduction;
    }
}
