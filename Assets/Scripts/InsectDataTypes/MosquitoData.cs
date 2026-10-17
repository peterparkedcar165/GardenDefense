using UnityEngine;

[CreateAssetMenu(fileName = "MosquitoData", menuName = "Scriptable Objects/InsectData/Mosquito")]
public class MosquitoData : InsectData
{
    [Header("Mosquito")]
    public float biteDPS = 12f;
    public float biteDuration = 4f;
    public float biteHealingReductionPercent = 0.5f;
    public float bloodsuckerHealPerSecond = 12f;
    public float bloodsuckerHealPercent = 0.02f;
}
