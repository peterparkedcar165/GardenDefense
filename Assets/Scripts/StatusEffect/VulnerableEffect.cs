using UnityEngine;

public class VulnerableEffect : StatusEffect
{
    public float shred;
    public VulnerableEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        shred = 0.32f * (1f + source.elementalAffinity);
        effectType = Type.negative;
        elementalType = ElementalType.Poison;
    }

    public override string GetName() => "<color=purple>Vulnerable</color>";
    public override string GetDescription() => $"Reduce <color=#9400D3><b>DoT Resistance</b></color> by <color=red><b>{shred * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Vulnerable", new Color(0.6f, 0.1f, 0.8f));

        Insect insect = (Insect)target;
        insect.dotResistanceAdder -= shred;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        Insect insect = (Insect)target;
        insect.dotResistanceAdder += shred;
    }
}
