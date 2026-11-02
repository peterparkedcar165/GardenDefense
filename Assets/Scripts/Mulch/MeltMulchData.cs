using UnityEngine;

[CreateAssetMenu(fileName = "MeltMulchData", menuName = "Scriptable Objects/Mulch/Melt")]
// requires Fire, Water AND Ice in the loadout (all three react together: Water+Ice freezes,
// Fire melts). replaces Entity.cs's base Freeze+Fire instant-shatter reaction with a gradual
// melt: every qualifying Fire hit against a Frozen target chips its remaining duration down
// instead of removing it outright, and that hit itself deals bonus damage - see
// MulchManager.MeltActive/MeltFreezeDurationReduction/MeltDamageMultiplier, read directly by
// Entity.cs
public class MeltMulchData : MulchData
{
    [Tooltip("seconds shaved off the target's remaining Freeze duration per qualifying Fire hit")]
    public float freezeDurationReduction = 0.2f;
    [Tooltip("damage multiplier on each qualifying Fire hit, e.g. 1.75 = 175% damage")]
    public float damageMultiplier = 1.75f;

    public override MulchEffect CreateEffect() => new MeltMulchEffect(this);

    public override string GetDescription() =>
        $"Dealing Fire Damage to a Frozen target reduces the Freeze duration by " +
        $"<b><color=green>{freezeDurationReduction:F1}s</color></b> per hit, but each hit deals " +
        $"<b><color=green>{damageMultiplier * 100f:F0}%</color></b> damage.";
}
