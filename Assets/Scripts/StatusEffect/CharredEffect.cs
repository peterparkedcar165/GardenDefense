using UnityEngine;

// fire resistance reduction, applied by groundthorn's grass tile bonus
public class CharredEffect : StatusEffect
{
    private readonly float reduction;

    public CharredEffect(Entity target, float duration, int level, Entity source, float reduction)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        this.reduction = reduction;
    }

    public override string GetName() => "<color=orange>Charred</color>";
    public override string GetDescription() =>
        $"<color=orange><b>Fire Resistance</b></color> reduced by <color=red><b>{reduction * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        target.fireResistanceAdder -= reduction;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Charred", new Color(1f, 0.5f, 0.2f));
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.fireResistanceAdder += reduction;
    }
}
