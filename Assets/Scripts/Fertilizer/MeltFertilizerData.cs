using UnityEngine;

[CreateAssetMenu(fileName = "MeltFertilizer", menuName = "Scriptable Objects/Fertilizer/Melt")]
// needs fire, water and ice in the loadout (water and ice freeze, fire melts). replaces
// Entity.Damage's freeze and fire instant shatter with a gradual melt: every qualifying fire hit on
// a frozen insect chips its freeze down instead of breaking it, and that hit deals bonus damage
// (FertilizerManager.MeltActive, MeltFreezeDurationReduction, MeltDamageMultiplier)
public class MeltFertilizerData : FertilizerData
{
    // its paper bag icon's symbol, the mulches' categories (proposal, Tommy, 2026-11-24: fire against frozen insects)
    protected override MulchSymbol DefaultSymbol => MulchSymbol.Affliction;

    [Tooltip("seconds shaved off the target's remaining Freeze duration per qualifying Fire hit")]
    public float freezeDurationReduction = 0.2f;
    [Tooltip("damage multiplier on each qualifying Fire hit, e.g. 1.75 = 175% damage")]
    public float damageMultiplier = 1.75f;

    public override FertilizerEffect CreateEffect() => new MeltFertilizerEffect(this);

    public override string GetDescription() =>
        $"Dealing Fire Damage to a Frozen target reduces the Freeze duration by " +
        $"<b><color=green>{freezeDurationReduction:F1}s</color></b> per hit, but each hit deals " +
        $"<b><color=green>{damageMultiplier * 100f:F0}%</color></b> damage.";
}
