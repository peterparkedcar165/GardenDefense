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
        float amount = healingPerSecond * share;
        HealTarget(amount);
        // the heal goes on after rhodiola dies or is uprooted; only a living rhodiola gets anything
        // back ("is" skips unity's destroyed check, so test it with != null as well): her own burgeon on
        // herself fills her golden sap (Tommy, 2026-11-27), and on another plant her max level passive
        // returns her a share
        if (source is Rhodiola rhodiola && rhodiola != null && rhodiola.IsAlive)
        {
            if (target == rhodiola) rhodiola.AddSap(amount);
            else rhodiola.ReturnMaxLevelHealing(amount);
        }
    }

    public override string GetName()        => "<color=green><b>Rejuvenating Burgeon</b></color>";
    public override string GetDescription() =>
        $"Recovering <color=green><b>{Mathf.RoundToInt(healingPerSecond)}</b></color> health every <color=green><b>{TickInterval:0.##}s</b></color>." +
        (armorBonus > 0f ? $" Grants <color=#00CED1><b>+{armorBonus:F0} Armor</b></color>." : "");
}
