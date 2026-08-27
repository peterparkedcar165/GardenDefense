using UnityEngine;

[CreateAssetMenu(fileName = "WinterMothData", menuName = "Scriptable Objects/InsectData/WinterMoth")]
public class WinterMothData : InsectData
{
    [Header("Winter Moth")]
    public float coldAuraRadius = 2f;
    public float coldPerSecond = 1.5f;
}
