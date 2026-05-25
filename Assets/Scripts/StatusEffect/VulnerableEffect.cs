using UnityEngine;

public class VulnerableEffect : StatusEffect
{
    public float shred;
    public VulnerableEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        shred = 0.32f * (1f + source.elementalPower);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#FF6347>Vulnerable</color>";
    public override string GetDescription() => $"Reduce DoT Resistance by <color=green><b>{shred * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        GameObject indicator = Object.Instantiate(Resources.Load<GameObject>("DamageIndicator"), target.transform.position + new Vector3(0.4f, 0f, 0f), Quaternion.identity);
        indicator.GetComponent<DamageIndicator>().Initialize("Vulnerable", new Color(0.6f, 0.1f, 0.8f));

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
