using UnityEngine;

// authored per level: after the given wave fully completes, the player's mid-level fertilizer
// queue (separate from the existing pre-level single fertilizer pick) grows by one entry of this
// rarity. this is the "randomized numbers" fertilizer flow - see FertilizerQueue/FertilizerManager
[System.Serializable]
public class FertilizerGrant
{
    [Tooltip("granted once this wave number fully completes")]
    public int afterWave;
    public FertilizerTier rarity;
}
