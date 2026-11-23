using UnityEngine;

public class RejuvenatingBurgeonEffect : RegenerationEffect
{
    // armor the plant holds for as long as the burgeon lasts, taken back when it ends or refreshes
    private readonly float armorBonus;

    public RejuvenatingBurgeonEffect(Entity target, float duration, int level, Entity source, float healPerTick, float tickInterval, float armorBonus = 0f)
        : base(target, duration, level, source,
               healingPerSecond: healPerTick,
               tickInterval: tickInterval)
    {
        elementalType = ElementalType.Grass;
        this.armorBonus = armorBonus;
    }

    public override void OnApply()
    {
        target.armorAdder += armorBonus;
    }

    public override void OnExpire()
    {
        target.armorAdder -= armorBonus;
    }

    protected override void HealTick(float share)
    {
        HealTarget(healingPerSecond * share);
        if (source is Rhodiola rhodiola) rhodiola.ReturnMaxLevelHealing(healingPerSecond * share);
    }

    public override string GetName()        => "<color=green><b>Rejuvenating Burgeon</b></color>";
    public override string GetDescription() =>
        $"Recovering <color=green><b>{Mathf.RoundToInt(healingPerSecond)}</b></color> health every <color=green><b>{TickInterval:0.##}s</b></color>." +
        (armorBonus > 0f ? $" Grants <color=#00CED1><b>+{armorBonus:F0} Armor</b></color>." : "");
}
