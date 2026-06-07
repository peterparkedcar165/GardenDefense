using UnityEngine;

[CreateAssetMenu(fileName = "JumpingSpiderData", menuName = "Scriptable Objects/InsectData/JumpingSpider")]
public class JumpingSpiderData : InsectData
{
    [Header("Jumping Spider")]
    public float leapRange      = 3f;
    public float aimDuration    = 0.5f;
    public float jumpUpVelocity = 7f;
    public float webDuration    = 2f;
}
