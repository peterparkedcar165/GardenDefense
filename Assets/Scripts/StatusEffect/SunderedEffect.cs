using UnityEngine;

// percentage armor reduction, applied by groundthorn attacks at passive max level
public class SunderedEffect : StatusEffect
{
    private readonly float percent;
    private float flatReduction;

    public SunderedEffect(Entity target, float duration, int level, Entity source, float percent)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        this.percent = percent;
    }

    public override string GetName() => "<color=#79391F>Sundered</color>";
    public override string GetDescription() =>
        $"<color=#00CED1><b>Armor</b></color> reduced by <color=red><b>{percent * 100f:F0}%</b></color>.";

    public override void OnApply()
    {
        flatReduction = Mathf.Max(0f, target.armor * percent);
        target.armorAdder -= flatReduction;
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Sundered", new Color(0.47f, 0.22f, 0.12f));
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.armorAdder += flatReduction;
    }
}
