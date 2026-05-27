using UnityEngine;

public class BlossomingEffect : StatusEffect
{
    private readonly float natureDamageBonus;
    private readonly float attackSpeedBonus;

    public BlossomingEffect(Entity target, float duration, int level, Entity source, float natureDamageBonus, float attackSpeedBonus)
        : base(target, duration, level, source)
    {
        this.natureDamageBonus = natureDamageBonus;
        this.attackSpeedBonus = attackSpeedBonus;
        effectType = Type.positive;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(0.4f, 0f, 0f), "Blossoming", new Color(0.3f, 1f, 0.2f));
        target.natureDamageAdder += natureDamageBonus;
        target.attackSpeedMultiplier += attackSpeedBonus;
    }

    public override void OnExpire()
    {
        target.natureDamageAdder -= natureDamageBonus;
        target.attackSpeedMultiplier -= attackSpeedBonus;
    }

    public override string GetName() => "<color=green>Blossoming</color>";
    public override string GetDescription() =>
        $"Increase Nature Power by <color=green><b>{natureDamageBonus * 100f:F0}%</b></color>, and Attack Speed by <color=green><b>{attackSpeedBonus * 100f:F0}%</b></color>.";
}
