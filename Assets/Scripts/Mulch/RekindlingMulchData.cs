using UnityEngine;

[CreateAssetMenu(fileName = "RekindlingMulchData", menuName = "Scriptable Objects/Mulch/Rekindling")]
public class RekindlingMulchData : MulchData
{
    [Tooltip("how much shorter every Burn's base duration becomes, e.g. 0.4 = 40% shorter")]
    public float durationReductionPercent = 0.25f;
    [Tooltip("seconds added to an already-Burning target's remaining duration per qualifying hit")]
    public float extendPerHit = 2f;

    public override MulchEffect CreateEffect() => new RekindlingMulchEffect(this);

    public override string GetDescription() =>
        $"Reduce base <b><color=orange>Burn</color></b> duration by <b><color=green>{durationReductionPercent * 100f:F0}%</color></b>. " +
        $"Dealing <b><color=green>Grass Damage</color></b> against a <b><color=orange>Burning</color></b> target extends the " +
        $"<b><color=orange>Burn</color></b> by <b><color=green>{extendPerHit:F0}</color></b> seconds.";
}
