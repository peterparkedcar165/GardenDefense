using UnityEngine;

// OldCarrot's Path1 max (the retired kit kept as a placeholder - current Carrot no longer uses
// this): each hit from a specific OldCarrot's attack OR its Psionic OldCarrot stacks a mark on
// the target, increasing that SAME OldCarrot's own subsequent damage on it by 5% per stack,
// uncapped, refreshing its duration on every hit so it fades if that OldCarrot stops hitting this
// target for a while. source-stackable: several different OldCarrots hitting the same insect
// each track their own independent mark rather than sharing one
public class PsionicMarkEffect : StatusEffect
{
    public const float DamagePerStack = 0.05f;

    public int stacks;
    private const float StackDuration = 5f;

    public PsionicMarkEffect(Entity target, Entity source)
        : base(target, StackDuration, 1, source)
    {
        effectType = Type.negative;
        elementalType = ElementalType.Ground;
        sourceStackable = true;
        visible = false;
    }

    public float DamageMultiplier => 1f + DamagePerStack * stacks;

    public void AddStack()
    {
        stacks++;
        duration = StackDuration;
    }

    public override void OnApply() => stacks = 1;
    public override void OnExpire() { }
    public override void OnTick(float deltaTime) { }

    public override string GetName() => $"<color=#B266FF><b>Psionic Mark</b></color> x{stacks}";
    public override string GetDescription() =>
        $"Takes <color=red><b>+{stacks * DamagePerStack * 100f:F0}%</b></color> damage from this Carrot's attacks and Psionic Carrots.";
}
