using UnityEngine;

// the next plant bought costs nothing (MulchManager.GetPlacementCost). that plant then sits with
// Plant.cannotTargetInsects on until its waves have passed: no insect targets, no damage or
// debuffs on insects, while heals, buffs and sun keep running. once the free plant is placed the
// mulch has paid out and can't be sold or swapped. a bud plant that dies and gets revived comes
// back as a new plant, already awake
public class BudMulchEffect : MulchEffect
{
    private static readonly Color DormantColor = new Color(0.55f, 0.85f, 0.45f);

    private readonly BudMulchData typedData;
    private bool pending = true;
    private Plant bud;
    private MulchWaveCounter waves;

    public BudMulchEffect(BudMulchData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate() {}
    public override void Deactivate() => Wake(false);

    public override bool GrantsFreePlacement => pending;
    public override bool CanBeRemoved => pending;

    public override void OnPlantBought(Plant plant, bool placedForFree)
    {
        if (!pending || !placedForFree || plant == null) return;
        pending = false;
        if (typedData.dormantWaves <= 0) return;

        bud = plant;
        waves = new MulchWaveCounter();
        plant.cannotTargetInsects = true;
        StatusIndicator.Spawn(plant.transform.position + new Vector3(0.4f, 0f, 0f), "Dormant", DormantColor);
    }

    public override void OnWaveStarted(int wave) => waves?.WaveStarted(wave);

    public override void OnWaveEnded(int wave)
    {
        if (waves == null || !waves.WaveEnded(wave)) return;
        if (waves.Completed >= typedData.dormantWaves) Wake(true);
    }

    private void Wake(bool announce)
    {
        if (bud != null)
        {
            bud.cannotTargetInsects = false;
            if (announce && bud.IsAlive)
                StatusIndicator.Spawn(bud.transform.position + new Vector3(0.4f, 0f, 0f), "Awake", DormantColor);
        }
        bud = null;
        waves = null;
    }
}
