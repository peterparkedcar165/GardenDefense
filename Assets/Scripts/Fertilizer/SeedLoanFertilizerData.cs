using UnityEngine;

[CreateAssetMenu(fileName = "SeedLoanFertilizer", menuName = "Scriptable Objects/Fertilizer/Seed Loan")]
public class SeedLoanFertilizerData : FertilizerData
{
    [Tooltip("Sun given the moment this fertilizer is picked")]
    public int loanAmount = 250;
    [Tooltip("Sun taken back when the loan comes due")]
    public int repayAmount = 320;
    [Tooltip("the loan comes due at the end of this many waves")]
    public int waves = 5;

    public override FertilizerEffect CreateEffect() => new SeedLoanFertilizerEffect(this);

    public override string GetDescription() =>
        $"Start the level with <b><color=green>{loanAmount}</color></b> more <b><color=#FFD700>Sun</color></b>. " +
        $"At the end of wave <b><color=red>{waves}</color></b>, <b><color=red>{repayAmount}</color></b> <b><color=#FFD700>Sun</color></b> is taken back. " +
        $"If you can't pay it all, the rest comes out of the next <b><color=#FFD700>Sun</color></b> you gain.";
}
