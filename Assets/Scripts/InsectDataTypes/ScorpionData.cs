using UnityEngine;

[CreateAssetMenu(fileName = "ScorpionData", menuName = "Scriptable Objects/InsectData/Scorpion")]
public class ScorpionData : InsectData
{
    [Header("Scorpion")]
    public float venomDPS      = 16f;
    public float venomDuration = 12f;
}
