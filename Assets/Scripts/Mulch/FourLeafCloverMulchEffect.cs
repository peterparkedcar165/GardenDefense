public class FourLeafCloverMulchEffect : MulchEffect
{
    private readonly FourLeafCloverMulchData typedData;
    private float applied;
    // rare and epic mulches its plant's kills dropped this level
    private int rareDrops;

    public FourLeafCloverMulchEffect(FourLeafCloverMulchData data, MulchItem item) : base(item)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (Owner == null) return;
        applied = V(typedData.rareItemDropRate);
        Owner.rareItemDropRate += applied;
    }

    public override void Deactivate()
    {
        if (Owner != null) Owner.rareItemDropRate -= applied;
        applied = 0f;
    }

    public override void OnKillDrop(MulchItem item, bool fromChance)
    {
        if (item != null && item.rarity >= MulchTier.Rare) rareDrops++;
    }

    public override string GetStatus() => $"Rare or Epic drops from its kills: {rareDrops}";
}
