using UnityEngine;

public class FractureEffect : StatusEffect
{
    public float bonusMultiplier;

    public FractureEffect(Entity target, float duration, int level, Entity source) : base(target, duration, level, source)
    {
        bonusMultiplier = 0.25f * (1f + source.elementalAffinity);
        effectType = Type.negative;
    }

    public override string GetName() => "<color=#00FFFF>Fracture</color>";
    public override string GetDescription() => $"Upon taking <color=#A0522D><b>Physical Damage</b></color>, deal an additional <color=orange><b>Fire Damage</b></color> instance equal to <color=green><b>{bonusMultiplier * 100f:F0}%</b></color> of the original damage.";

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Fracture", new Color(0f, 1f, 1f));
    }

    public override void OnTick(float deltaTime) { }
    public override void OnExpire() { }
}
