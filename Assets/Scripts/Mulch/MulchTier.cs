// mulch rarity. unlike FertilizerTier, this does not control a stat magnitude multiplier -
// there is no such thing as the same mulch existing at a lower or higher tier with different
// numbers. the tier is just which draft bucket (see MulchGrant) a mulch belongs to
public enum MulchTier
{
    Common,
    Rare,
    Epic,
    Legendary
}
