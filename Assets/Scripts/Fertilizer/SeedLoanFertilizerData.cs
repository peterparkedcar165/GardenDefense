using UnityEngine;

[CreateAssetMenu(fileName = "SeedLoanFertilizer", menuName = "Scriptable Objects/Fertilizer/Solar Loan")]
public class SeedLoanFertilizerData : FertilizerData
{
    [Tooltip("Sun given the moment this fertilizer is picked")]
    public int loanAmount = 100;
    [Tooltip("Sun taken back when the loan comes due")]
    public int repayAmount = 200;
    [Tooltip("the loan comes due at the end of this many waves")]
    public int waves = 3;

    public override FertilizerEffect CreateEffect() => new SeedLoanFertilizerEffect(this);

    public override string GetDescription() =>
        $"Start the level with {Green(loanAmount.ToString())} additional {SunLabel}, " +
        $"but lose {Red(repayAmount.ToString())} after {Green(waves.ToString())} {(waves == 1 ? "wave" : "waves")}.";
}
