using UnityEngine;

// authored per level: after the given wave fully completes, the player's mulch queue grows by
// one entry of this tier. same shape as FertilizerGrant - see MulchManager
[System.Serializable]
public class MulchGrant
{
    [Tooltip("granted once this wave number fully completes")]
    public int afterWave;
    public MulchTier tier;
}
