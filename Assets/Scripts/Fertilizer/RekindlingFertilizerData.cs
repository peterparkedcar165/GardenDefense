using UnityEngine;

[CreateAssetMenu(fileName = "RekindlingFertilizer", menuName = "Scriptable Objects/Fertilizer/Rekindling")]
public class RekindlingFertilizerData : FertilizerData
{
    // its paper bag icon's symbol, the mulches' categories (proposal, Tommy, 2026-11-24: burn and grass against burning insects)
    protected override MulchSymbol DefaultSymbol => MulchSymbol.Affliction;

    [Tooltip("how much shorter every Burn's base duration becomes, e.g. 0.25 = 25% shorter")]
    public float durationReductionPercent = 0.25f;
    [Tooltip("seconds added to an already Burning target's remaining duration per qualifying hit")]
    public float extendPerHit = 1f;

    public override FertilizerEffect CreateEffect() => new RekindlingFertilizerEffect(this);

    public override string GetDescription() =>
        $"Reduce base <b><color=orange>Burn</color></b> duration by <b><color=green>{durationReductionPercent * 100f:F0}%</color></b>. " +
        $"Dealing <b><color=green>Grass Damage</color></b> against a <b><color=orange>Burning</color></b> target extends the " +
        $"<b><color=orange>Burn</color></b> by <b><color=green>{extendPerHit:0.#}</color></b> {(Mathf.Approximately(extendPerHit, 1f) ? "second" : "seconds")}.";
}
