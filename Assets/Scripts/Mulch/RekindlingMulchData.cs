using UnityEngine;

[CreateAssetMenu(fileName = "RekindlingMulchData", menuName = "Scriptable Objects/Mulch/Rekindling")]
public class RekindlingMulchData : MulchData
{
    [Tooltip("how much shorter every Burn's base duration becomes, e.g. 0.4 = 40% shorter")]
    public float durationReductionPercent = 0.4f;
    [Tooltip("seconds added to an already-Burning target's remaining duration per qualifying hit")]
    public float extendPerHit = 2f;

    public override MulchEffect CreateEffect() => new RekindlingMulchEffect(this);
}
