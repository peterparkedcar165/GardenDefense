using UnityEngine;

// grass elemental effect proc: reduces magic resistance, scaled by the source's elemental affinity
public class SeededEffect : StatusEffect, IElementalAffinityEffect
{
    private float shred;
    private float magicArmorReduction;
    private float damageReduction;

    public float AffinityPower => source?.elementalAffinity ?? 0f;

    public SeededEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        shred = 0.2f * (1f + source.elementalAffinity);
        magicArmorReduction = 100f * shred / (1f - Mathf.Min(shred, 0.99f));
        damageReduction = 0.15f * (1f + source.elementalAffinity);
        effectType = Type.negative;
        elementalType = ElementalType.Grass;
    }

    public override string GetName() => "<color=green>Seeded</color>";
    public override string GetDescription() =>
        $"Reduce <color=#00CED1><b>Magic Resistance</b></color> by <color=red><b>{shred * 100f:F0}%</b></color>. Reduce <color=green><b>Attack Damage</b></color> by <color=red><b>{damageReduction * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Seeded", new Color(0.3f, 0.7f, 0.3f));

        Insect insect = (Insect)target;
        insect.magicArmorAdder -= magicArmorReduction;
        insect.attackDamageMultiplier -= damageReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.magicArmorAdder += magicArmorReduction;
        insect.attackDamageMultiplier += damageReduction;
    }
}
