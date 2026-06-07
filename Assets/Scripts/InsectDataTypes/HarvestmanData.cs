using UnityEngine;

[CreateAssetMenu(fileName = "HarvestmanData", menuName = "Scriptable Objects/InsectData/Harvestman")]
public class HarvestmanData : InsectData
{
    [Header("Harvestman")]
    [Tooltip("how far above the visual the aim point sits (0 = default ground-level)")]
    public float aimPointHeight;
    [Tooltip("bonus damage multiplier against friendly insects (minions, hypnotized). 0 = no bonus")]
    public float minionDamageBonus;
}
