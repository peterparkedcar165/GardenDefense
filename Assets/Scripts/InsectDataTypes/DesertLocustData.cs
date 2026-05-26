using UnityEngine;

[CreateAssetMenu(fileName = "DesertLocustData", menuName = "Scriptable Objects/InsectData/DesertLocust")]
public class DesertLocustData : InsectData
{
    [Header("Devour")]
    [Range(0f, 1f)]
    public float devourReductionPercent = 1f;
}
