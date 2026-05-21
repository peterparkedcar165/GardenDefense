using UnityEngine;

[CreateAssetMenu(fileName = "FireAntData", menuName = "Scriptable Objects/InsectData/FireAnt")]
public class FireAntData : InsectData
{
    [Header("Fire Ant")]
    public float tempIncreasePerHit = 4f;
}
