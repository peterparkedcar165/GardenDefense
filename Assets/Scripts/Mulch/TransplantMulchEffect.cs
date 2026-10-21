// same static-flag convention as MulchManager's global multipliers (BurnDurationMultiplier, etc.)
// - IsActive gates the Transplant button's visibility in PlantUpgradeUI, CooldownSeconds is read
// directly by Plant.TriggerTransplant when resetting its per-plant cooldown timer. no per-plant
// push needed, unlike FamilySkillCooldownMulchEffect/RapidStrikesMulchEffect - every plant always
// has transplantCooldownTimer, it's just meaningless (and the button hidden) while this is off
public class TransplantMulchEffect : MulchEffect
{
    public static bool IsActive;
    public static float CooldownSeconds = 300f;

    private readonly TransplantMulchData typedData;

    public TransplantMulchEffect(TransplantMulchData data) : base(data)
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
        CooldownSeconds = 300f;
    }
}
