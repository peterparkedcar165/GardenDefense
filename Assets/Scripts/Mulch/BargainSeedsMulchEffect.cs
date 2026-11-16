// the discount belongs to a wave number (GameManager.currentWave), so the rest period after a
// wave still counts as that wave: one cheap plant per wave, and a new one the moment the next
// wave starts. a plant bud made free doesn't use it up
public class BargainSeedsMulchEffect : MulchEffect
{
    private readonly BargainSeedsMulchData typedData;
    private int usedOnWave = -1;

    public BargainSeedsMulchEffect(BargainSeedsMulchData data) : base(data)
    {
        typedData = data;
    }

    private static int CurrentWave => GameManager.instance != null ? GameManager.instance.currentWave : 0;
    private bool Available => usedOnWave != CurrentWave;

    public override void Activate() => usedOnWave = -1;
    public override void Deactivate() {}

    public override int PlacementDiscount => Available ? typedData.discount : 0;

    public override string GetStatus() => Available ? "Discount ready this wave" : "Discount used this wave";

    public override void OnPlantBought(Plant plant, bool placedForFree)
    {
        if (!placedForFree && Available) usedOnWave = CurrentWave;
    }
}
