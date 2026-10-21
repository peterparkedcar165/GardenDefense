using UnityEngine;

// shared frame/background color per rarity tier, consistent across every mulch card regardless
// of the mulch's own element or authored MulchData.color - Common=green, Rare=blue, Epic=pink,
// Legendary=gold (not specified when this was requested, picked as the standard 4th-tier color)
public static class MulchTierColors
{
    public static Color Get(MulchTier tier) => tier switch
    {
        MulchTier.Common    => new Color(0.3f, 1f, 0.2f),
        MulchTier.Rare      => new Color(0.2f, 0.5f, 1f),
        MulchTier.Epic      => new Color(1f, 0.3f, 0.8f),
        MulchTier.Legendary => new Color(1f, 0.75f, 0.1f),
        _                   => Color.white
    };
}
