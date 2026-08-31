using UnityEngine;

// Carrot's Path1 max: each hit from a specific Carrot's attack OR its Psionic Carrot stacks a
// mark on the target, increasing that SAME Carrot's own subsequent damage on it by 3% per
// stack, uncapped, refreshing its duration on every hit so it fades if that Carrot stops
// hitting this target for a while. source-stackable: several different Carrots hitting the
// same insect each track their own independent mark rather than sharing one
public class PsionicMarkEffect : StatusEffect
{
    public const float DamagePerStack = 0.03f;

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
