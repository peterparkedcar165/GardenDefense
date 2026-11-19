using UnityEngine;
using System.Collections.Generic;

// a tool, not a lasting mulch: picking it opens the owned mulch picker, the chosen mulch is taken
// away (MulchManager.RemoveMulch undoes it), and a replacement draft of equal or higher rarity
// opens straight away (MulchManager.GrantSwapDraft). the tool itself is spent
[CreateAssetMenu(fileName = "SeedSwapMulchData", menuName = "Scriptable Objects/Mulch/Seed Swap")]
public class SeedSwapMulchData : MulchData
{
    [Tooltip("choices in the replacement draft from the swapped mulch's own tier")]
    public int sameTierChoices = 2;
    [Tooltip("choices in the replacement draft from the tier above it. a slot no mulch can fill " +
        "is filled from the same tier instead, never from a lower one")]
    public int higherTierChoices = 1;

    public override MulchEffect CreateEffect() => null;
    public override bool NeedsOwnedMulchTarget => true;
    public override bool OpensDraftAfterUse => true;

    // needs something it's allowed to swap
    public override bool MeetsExtraRequirements(List<PlantData> loadout, int currentWave) =>
        MulchManager.instance != null && MulchManager.instance.HasRemovableMulch(this);

    public override void UseOnTarget(MulchData target)
    {
        if (target != null)
            MulchManager.instance?.GrantSwapDraft(target.tier, sameTierChoices, higherTierChoices);
    }

    public override string TargetPrompt => "Pick a mulch to swap";
    public override string DescribeTargetUse(MulchData target) => $"Swap for a {TierLabel(target.tier)} or better mulch";

    public override string GetDescription() =>
        $"One time use: remove a mulch you own, then draft its replacement from " +
        $"<b><color=green>{sameTierChoices}</color></b> {(sameTierChoices == 1 ? "choice" : "choices")} of the same rarity and " +
        $"<b><color=green>{higherTierChoices}</color></b> of the rarity above. Mulches that already paid out can't be swapped.";
}
