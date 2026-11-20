using UnityEngine;

// one colour per rarity, shared by mulch icons (grayscale art tinted by this), their slot borders,
// the little squares by a plant's health bar, and fertilizer cards: Common green, Rare blue, Epic pink
public static class MulchTierColors
{
    public static Color Get(MulchTier tier) => tier switch
    {
        MulchTier.Common => new Color(0.3f, 1f, 0.2f),
        MulchTier.Rare   => new Color(0.2f, 0.5f, 1f),
        MulchTier.Epic   => new Color(1f, 0.3f, 0.8f),
        _                => Color.white
    };

    public static Color Get(FertilizerTier tier) => Get((MulchTier)(int)tier);
}
