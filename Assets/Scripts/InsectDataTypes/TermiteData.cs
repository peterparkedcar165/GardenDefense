using UnityEngine;

[CreateAssetMenu(fileName = "TermiteData", menuName = "Scriptable Objects/InsectData/Termite")]
public class TermiteData : InsectData
{
    [Header("Termite")]
    public float bonusPerTermite = 3f;
    public float swarmRange = 3f;
    [Min(0)] public int companionCount = 2;
}
