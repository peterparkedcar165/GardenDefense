using UnityEngine;

// applied to the Shooter Carrot is currently bonded to (Soil Bond). continuously grants Attack
// Range equal to 25% of Carrot's own CURRENT total Attack Range, recalculated every tick so it
// tracks Carrot leveling up or gaining range from other sources while still bonded - rather than
// a fixed effect duration racing a reapply interval, this is permanent and removes itself (and
// unwinds its own bonus) the instant its source Carrot is gone, matching PsionicBondEffect's
// approach. at Carrot's Path2 max, also grants this plant +25% Attack Speed.
//
// source-stackable: several Carrots could all bond to the same Shooter at once, each carrying
// its own instance here rather than the newest bond overwriting the last - each Carrot removes
// only its own instance via Entity.RemoveEffect<T>(source), never the others'
public class SoilBondEffect : StatusEffect
{
    private readonly Shooter carrot;
    private float appliedRangeBonus;
    private bool maxBonusActive;
    public const float RangeBonusFraction = 0.25f;
    public const float MaxLevelAttackSpeedBonus = 0.25f;

    public SoilBondEffect(Entity target, Entity source, Shooter carrot)
        : base(target, float.MaxValue, 1, source)
    {
        this.carrot = carrot;
        effectType = Type.positive;
        elementalType = ElementalType.Ground;
        sourceStackable = true;
    }

    public override void OnApply() { }

    public override void OnTick(float deltaTime)
    {
        if (carrot == null || !carrot.IsAlive)
        {
            duration = 0f;
            return;
        }

        float desiredRangeBonus = carrot.attackRange * RangeBonusFraction;
        if (!Mathf.Approximately(desiredRangeBonus, appliedRangeBonus))
        {
            target.attackRangeAdder += desiredRangeBonus - appliedRangeBonus;
            appliedRangeBonus = desiredRangeBonus;
        }

        bool shouldMaxBonus = carrot is Plant carrotPlant && carrotPlant.IsPath2Maxed;
        if (shouldMaxBonus != maxBonusActive)
        {
            maxBonusActive = shouldMaxBonus;
            target.attackSpeedMultiplier += maxBonusActive ? MaxLevelAttackSpeedBonus : -MaxLevelAttackSpeedBonus;
        }
    }

    public override void OnExpire()
    {
        if (appliedRangeBonus != 0f) target.attackRangeAdder -= appliedRangeBonus;
        if (maxBonusActive) target.attackSpeedMultiplier -= MaxLevelAttackSpeedBonus;
    }

    public override string GetName() => "<color=#B87333><b>Soil Bond</b></color>";
    public override string GetDescription()
    {
        string s = $"Linked to {(carrot != null ? carrot.GetName() : "a Carrot")}: gains <color=green><b>+{appliedRangeBonus:F1}</b></color> Attack Range.";
        if (maxBonusActive)
            s += $"\nGains <color=green><b>+{MaxLevelAttackSpeedBonus * 100f:F0}%</b></color> Attack Speed.";
        return s;
    }
}
