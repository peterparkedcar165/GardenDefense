using UnityEngine;

[CreateAssetMenu(fileName = "HealSplashMulch", menuName = "Scriptable Objects/Mulch/Heal Splash")]
public class HealSplashMulchData : MulchData
{
    [Tooltip("damage dealt to nearby insects, as a share of the healing done")]
    public TierValue damagePercent = TierValue.Scaled(0.5f);
    [Tooltip("radius around the healed plant, in tiles")]
    public float radius = 1f;
    [Tooltip("seconds between splashes, so a fast ticking heal can't spam it")]
    public float internalCooldown = 1f;

    public override MulchEffect CreateEffect(MulchItem item) => new HealSplashMulchEffect(this, item);

    public override string GetDescription(MulchItem item)
    {
        MulchTier r = item.rarity;
        return $"Whenever this plant heals <b>another</b> plant, insects near that plant take damage equal to " +
            $"{Green(Pct(damagePercent.Get(r)))} of the healing done.";
    }
}
