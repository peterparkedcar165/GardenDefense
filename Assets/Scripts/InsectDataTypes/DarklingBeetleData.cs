using UnityEngine;

[CreateAssetMenu(fileName = "DarklingBeetleData", menuName = "Scriptable Objects/InsectData/DarklingBeetle")]
public class DarklingBeetleData : InsectData
{
    [Header("Carry")]
    public float carryPickupRange = 1.5f;
    public float carryPickupCheckInterval = 1f;
    public float carryPickupDelay = 0.5f;
    public float carryPickupHeight = 0.4f;
}
