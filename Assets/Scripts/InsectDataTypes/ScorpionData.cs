using UnityEngine;

[CreateAssetMenu(fileName = "ScorpionData", menuName = "Scriptable Objects/InsectData/Scorpion")]
public class ScorpionData : InsectData
{
    [Header("Scorpion")]
    public float venomDPS      = 5f;
    public float venomDuration = 4f;
}
