using UnityEngine;

[CreateAssetMenu(fileName = "FruitFlyData", menuName = "Scriptable Objects/InsectData/Fruit Fly")]
public class FruitFlyData : InsectData
{
    [Header("Fruit Fly")]
    public float bonusPerFruitFly = 12f;
    public float swarmRange = 3f;
    [Min(0)] public int companionCount = 2;
}
