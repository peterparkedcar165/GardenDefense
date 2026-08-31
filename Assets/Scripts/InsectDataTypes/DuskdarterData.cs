using UnityEngine;

[CreateAssetMenu(fileName = "DuskdarterData", menuName = "Scriptable Objects/InsectData/Duskdarter")]
public class DuskdarterData : InsectData
{
    [Header("Nocturnal")]
    public float darkMovementSpeedBonus = 0.25f;
    public float darkEvasionBonus = 0.15f;

    [Header("Carry")]
    public float carryPickupRange = 3f;
    public float carryPickupCheckInterval = 1f;
    public float carryPickupDelay = 0.5f;
}
