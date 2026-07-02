using UnityEngine;

[CreateAssetMenu(fileName = "SnowAntData", menuName = "Scriptable Objects/InsectData/SnowAnt")]
public class SnowAntData : InsectData
{
    [Header("Snow Ant")]
    public float tempDecreasePerHit = 4f;
}
