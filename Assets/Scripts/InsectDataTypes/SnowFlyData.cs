using UnityEngine;

[CreateAssetMenu(fileName = "SnowFlyData", menuName = "Scriptable Objects/InsectData/SnowFly")]
public class SnowFlyData : InsectData
{
    [Header("Snow Fly")]
    public float snowArmorBonus = 25f;
}
