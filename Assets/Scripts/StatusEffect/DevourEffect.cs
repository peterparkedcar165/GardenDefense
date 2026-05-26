using UnityEngine;

public class DevourEffect : StatusEffect
{
    private float totalReduction = 0f;
    private float baseDuration;

    public DevourEffect(Entity target, float duration, int level, Entity source)
        : base(target, duration, level, source)
    {
        effectType = Type.negative;
        baseDuration = duration;
    }

    public override void OnApply() { }

    // called on each locust attack; stacks the reduction and refreshes the timer
    public void AddStack(float amount)
    {
        totalReduction += amount;
        target.maxHealth -= amount;
        target.health = Mathf.Min(target.health, target.maxHealth);
        duration = baseDuration;
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.maxHealth += totalReduction;
        totalReduction = 0f;
    }

    public override string GetName() => "<color=#8B6914>Devoured</color>";
    public override string GetDescription() =>
        $"Max Health reduced by <color=#8B6914><b>{totalReduction:F0}</b></color>.";
}
