using UnityEngine;
using System.Collections.Generic;

// a tool, not a lasting mulch: picking it opens the owned mulch picker, the chosen mulch is taken
// away (MulchManager.RemoveMulch undoes it), its price in sun is paid, and the tool is spent
[CreateAssetMenu(fileName = "SeedSellMulchData", menuName = "Scriptable Objects/Mulch/Seed Sell")]
public class SeedSellMulchData : MulchData
{
    [Tooltip("sun paid for selling a mulch of each tier")]
    public int commonPrice = 50;
    public int rarePrice = 100;
    public int epicPrice = 175;
    public int legendaryPrice = 250;

    public int PriceFor(MulchTier tier) => tier switch
    {
        MulchTier.Common    => commonPrice,
        MulchTier.Rare      => rarePrice,
        MulchTier.Epic      => epicPrice,
        MulchTier.Legendary => legendaryPrice,
        _                   => 0
    };

    public override MulchEffect CreateEffect() => null;
    public override bool NeedsOwnedMulchTarget => true;
    // mulches that already paid out can still be sold (tommy, 2026-11-19): they keep what they gave
    public override bool TakesPaidOutMulches => true;

    // needs something it's allowed to sell
    public override bool MeetsExtraRequirements(List<PlantData> loadout, int currentWave) =>
        MulchManager.instance != null && MulchManager.instance.HasRemovableMulch(this);

    public override void UseOnTarget(MulchData target)
    {
        int price = target != null ? PriceFor(target.tier) : 0;
        if (price > 0) GameManager.instance?.AddSun(price);
    }

    public override string TargetPrompt => "Pick a mulch to sell";
    public override string DescribeTargetUse(MulchData target) =>
        $"Sell for <b><color=green>{PriceFor(target.tier)}</color></b> <b><color=#FFD700>Sun</color></b>";

    public override string GetDescription() =>
        $"One time use: remove a mulch you own and gain <b><color=#FFD700>Sun</color></b> for it. " +
        $"{TierLabel(MulchTier.Common)} <b><color=green>{commonPrice}</color></b>, {TierLabel(MulchTier.Rare)} <b><color=green>{rarePrice}</color></b>, " +
        $"{TierLabel(MulchTier.Epic)} <b><color=green>{epicPrice}</color></b>. Mulches that already paid out can still be sold, and what they gave stays.";
}
