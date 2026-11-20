// IsActive shows the Transplant button in PlantUpgradeUI, CooldownSeconds is read by
// Plant.TriggerTransplant when it restarts that plant's own cooldown
public class TransplantFertilizerEffect : FertilizerEffect
{
    public static bool IsActive;
    public static float CooldownSeconds = 60f;

    private readonly TransplantFertilizerData typedData;

    public TransplantFertilizerEffect(TransplantFertilizerData data) : base(data)
    {
        typedData = data;
    }

    public override void Activate()
    {
        IsActive = true;
        CooldownSeconds = typedData.cooldownSeconds;
    }

    public override void Deactivate()
    {
        IsActive = false;
        CooldownSeconds = 60f;
    }
}
