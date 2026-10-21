using UnityEngine;

[CreateAssetMenu(fileName = "RejuvenatorMulchData", menuName = "Scriptable Objects/Mulch/Rejuvenator")]
public class RejuvenatorMulchData : MulchData
{
    [Tooltip("how much stronger every Regeneration-tagged heal becomes, e.g. 0.35 = 35% more effective")]
    public float effectivenessBonus = 0.35f;

    public override MulchEffect CreateEffect() => new RejuvenatorMulchEffect(this);
}
