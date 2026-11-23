using UnityEngine;

// Mosquito's self-heal, applied to itself the moment Mosquito Bite lands on its victim -
// heals a flat amount plus a percent of its own max health per second, for as long as the
// bite it came from lasts
public class BloodsuckerEffect : RegenerationEffect
{
    private readonly float healPercent;

    public BloodsuckerEffect(Entity target, float duration, int level, Entity source, float healingPerSecond, float healPercent)
        : base(target, duration, level, source, healingPerSecond)
    {
        this.healPercent = healPercent;
        elementalType = ElementalType.Grass;
    }

    public override void OnApply()
    {
        StatusIndicator.Spawn(target.transform.position + new Vector3(-0.4f, 0f, 0f), "Bloodsucker", new Color(0.55f, 0.05f, 0.05f));
    }

    // the flat part is fixed when it lands, the max health part is read as each tick lands
    protected override void HealTick(float share) =>
        target.Heal((healingPerSecond + target.maxHealth * healPercent) * share, source);

    public override string GetName() => "<color=#8B0000>Bloodsucker</color>";
    public override string GetDescription() =>
        $"Recovering <color=#88FF88><b>{healingPerSecond:F0}</b></color> + <color=#88FF88><b>{healPercent * 100f:F0}%</b></color> max health per second.";
}
