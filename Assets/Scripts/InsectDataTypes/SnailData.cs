using UnityEngine;

[CreateAssetMenu(fileName = "SnailData", menuName = "Scriptable Objects/InsectData/Snail")]
public class SnailData : InsectData
{
    [Header("Snail")]
    // equivalent to 60% Physical Resistance (armor / (150 + armor) = 0.6 at armor = 225,
    // using Entity.ArmorCoefficient = 150)
    public float armorBonusWhileShielded = 225f;
    public float moveSpeedBonusWhileUnshielded = 0.5f;
}
