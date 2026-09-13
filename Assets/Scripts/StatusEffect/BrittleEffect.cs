using UnityEngine;

public class BrittleEffect : StatusEffect
{
    public float bonusDamage;
    private float cachedelementalAffinity;
    private float shieldToughnessReduction;

    public BrittleEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        cachedelementalAffinity = source?.elementalAffinity ?? 0f;
        bonusDamage = 11f * (1f + cachedelementalAffinity);
        shieldToughnessReduction = 0.25f * (1f + cachedelementalAffinity);
        effectType = Type.negative;
        elementalType = ElementalType.Ice;
    }

    public override string GetName() => "<color=#00FFFF>Brittle</color>";
    public override string GetDescription() =>
        $"Inflict <color=green><b>{bonusDamage:F0}</b></color> additional damage upon being hurt. " +
        $"Reduce <color=#00CED1><b>Shield Toughness</b></color> by <color=red><b>{shieldToughnessReduction * 100:F0}%</b></color>. " +
        $"(11 × (1 + <color=green><b>{cachedelementalAffinity * 100:F0}% Elemental Affinity</b></color>))";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Brittle", Color.green);
        target.shieldToughnessAdder -= shieldToughnessReduction;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.shieldToughnessAdder += shieldToughnessReduction;
    }
}
