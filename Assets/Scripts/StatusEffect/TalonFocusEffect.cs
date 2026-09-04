using UnityEngine;

// Bird of Paradise's own stacking self-buff: every attack that lands (primary target or any
// cleave target while Three Talon Strike is active) refreshes and adds a stack, up to the
// current cap (grows with Path1 level, reaching 6 at Path1 max). each stack grants Attack
// Speed; at Path2 max, each stack also grants Armor Shred; at Path1 max, sitting at the full
// stack cap also grants a flat Total Attack Speed bonus. duration refreshes to 2s on every new
// stack, so it only decays after a 2s gap without landing a hit
public class TalonFocusEffect : StatusEffect
{
    private readonly BirdOfParadise bird;
    public int stacks;
    private float appliedASBonus;
    private float appliedArmorShred;
    private bool maxStacksBonusActive;

    public const float StackDuration = 2f;

    public TalonFocusEffect(Entity target, BirdOfParadise bird)
        : base(target, StackDuration, 1, bird)
    {
        this.bird = bird;
        effectType    = Type.positive;
        elementalType = ElementalType.Wind;
    }

    public override void OnApply()
    {
        stacks = 0;
        AddStack();
    }

    public override void OnTick(float deltaTime) { }

    public override void OnExpire()
    {
        target.attackSpeedMultiplier -= appliedASBonus;
        target.armorPenPercentAdder  -= appliedArmorShred;
        if (maxStacksBonusActive) target.attackSpeedTotalMultiplier -= bird?.MaxStacksAttackSpeedBonus ?? 0f;
    }

    // called once per landed hit (see Entity.HandleOnHitEffects) - adds a stack up to the
    // current cap and reapplies the resulting Attack Speed / Armor Shred / max-stacks bonuses
    public void AddStack()
    {
        int cap = bird != null ? bird.TalonFocusCap : 1;
        stacks = Mathf.Min(stacks + 1, cap);
        level = stacks; // StatusEffectPanel shows this generically as "Name [level]"
        duration = StackDuration;

        float asPerStack = bird?.TalonFocusASPerStack ?? 0.04f;
        float desiredAS = asPerStack * stacks;
        target.attackSpeedMultiplier += desiredAS - appliedASBonus;
        appliedASBonus = desiredAS;

        float shredPerStack = (bird != null && bird.IsPath2Maxed) ? bird.ArmorShredPerStack : 0f;
        float desiredShred = shredPerStack * stacks;
        target.armorPenPercentAdder += desiredShred - appliedArmorShred;
        appliedArmorShred = desiredShred;

        bool shouldHaveMaxBonus = bird != null && bird.IsPath1Maxed && stacks >= cap;
        if (shouldHaveMaxBonus != maxStacksBonusActive)
        {
            maxStacksBonusActive = shouldHaveMaxBonus;
            float bonus = bird?.MaxStacksAttackSpeedBonus ?? 0f;
            target.attackSpeedTotalMultiplier += maxStacksBonusActive ? bonus : -bonus;
        }
    }

    public override string GetName() => "<color=#B2EBF2><b>Talon Focus</b></color>";
    public override string GetDescription()
    {
        string desc = $"Increase <color=green><b>Attack Speed</b></color> by <color=green><b>{appliedASBonus * 100f:F0}%</b></color>.";
        if (appliedArmorShred > 0f)
            desc += $" Increase <color=green><b>Armor Shred</b></color> by <color=green><b>{appliedArmorShred * 100f:F0}%</b></color>.";
        if (maxStacksBonusActive)
            desc += $" Increase <color=green><b>Total Attack Speed</b></color> by <color=green><b>{(bird?.MaxStacksAttackSpeedBonus ?? 0f) * 100f:F0}%</b></color>.";
        return desc;
    }
}
