using UnityEngine;

// solar loan (class name kept from seed loan): pays the loan on pick, then takes the repayment at the end of its last wave. whatever the bank
// can't cover becomes FertilizerManager.SunDebt, which GameManager.AddSun pays off first
// (proposal: the vault only says the sun is taken back)
public class SeedLoanFertilizerEffect : FertilizerEffect
{
    private readonly SeedLoanFertilizerData typedData;
    private readonly WaveCounter waves = new WaveCounter();
    private bool due;

    public SeedLoanFertilizerEffect(SeedLoanFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        if (typedData.loanAmount > 0) GameManager.instance?.AddSun(typedData.loanAmount);
    }

    public override void Deactivate() {}

    public override void OnWaveStarted(int wave) => waves.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (due || !waves.WaveEnded(wave) || waves.Completed < typedData.waves) return;
        due = true;
        GameManager gm = GameManager.instance;
        if (gm == null) return;
        int paid = Mathf.Min(gm.SunCount, typedData.repayAmount);
        gm.SunCount -= paid;
        gm.UpdateSun();
        FertilizerManager.SunDebt += typedData.repayAmount - paid;
        StatusIndicator.Spawn(Vector3.zero, $"Solar Loan: -{typedData.repayAmount} Sun", new Color(1f, 0.45f, 0.3f));
    }

    public override string GetStatus()
    {
        if (!due)
        {
            int left = waves.Left(typedData.waves);
            return $"{typedData.repayAmount} Sun due in {left} {(left == 1 ? "wave" : "waves")}";
        }
        return FertilizerManager.SunDebt > 0 ? $"Still owed: {FertilizerManager.SunDebt} Sun" : "Paid back";
    }
}
