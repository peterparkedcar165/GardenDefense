using UnityEngine;

[CreateAssetMenu(fileName = "CleanGardenMulchData", menuName = "Scriptable Objects/Mulch/Clean Garden")]
public class CleanGardenMulchData : MulchData
{
    [Tooltip("whole waves in a row with no leaked insect needed for each free draft")]
    public int cleanWavesNeeded = 3;
    [Tooltip("tier of the free mulch draft each streak grants")]
    public MulchTier rewardTier = MulchTier.Common;

    public override MulchEffect CreateEffect() => new CleanGardenMulchEffect(this);

    public override string GetDescription() =>
        $"Every <b><color=green>{cleanWavesNeeded}</color></b> waves in a row with no leaked insect, gain a free " +
        $"{TierLabel(rewardTier)} mulch draft. A leak resets the count.";
}
