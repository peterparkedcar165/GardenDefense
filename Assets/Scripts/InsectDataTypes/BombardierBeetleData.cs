using UnityEngine;

[CreateAssetMenu(fileName = "BombardierBeetleData", menuName = "Scriptable Objects/InsectData/BombardierBeetle")]
public class BombardierBeetleData : InsectData
{
    [Header("Bombardier Beetle")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 4f;
    public float attackChargeTime = 0.75f;
    public float postFireDelay = 0.35f;
    public float splashDamagePercent = 0.5f;
    public float splashRadius = 1f;
    public float scorchChance = 0.5f;
    public float scorchDuration = 8f;
}
