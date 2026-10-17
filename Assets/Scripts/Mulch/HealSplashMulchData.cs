using UnityEngine;

[CreateAssetMenu(fileName = "HealSplashMulchData", menuName = "Scriptable Objects/Mulch/Heal Splash")]
public class HealSplashMulchData : MulchData
{
    [Tooltip("damage dealt to nearby insects, as a percent of the healing done")]
    public float damagePercent = 0.5f;
    public float radius = 1f;
    [Tooltip("per-healer internal cooldown, so one plant healing repeatedly can't spam this")]
    public float internalCooldown = 1f;

    public override MulchEffect CreateEffect() => new HealSplashMulchEffect(this);
}
