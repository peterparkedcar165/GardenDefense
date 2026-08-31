using UnityEngine;

[CreateAssetMenu(fileName = "EarthwormData", menuName = "Scriptable Objects/InsectData/Earthworm")]
public class EarthwormData : InsectData
{
    [Header("Burrow")]
    public float burrowDuration = 4f;
    public float burrowCooldown = 6f;
    public float tunnelOpenDuration = 20f;
}
