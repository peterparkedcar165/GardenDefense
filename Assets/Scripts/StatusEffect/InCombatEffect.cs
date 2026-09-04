// generic, invisible marker refreshed on both the source and target of every Damage() call (see
// Entity.RefreshCombatState), lasting Duration seconds. carries no stat effect of its own - it is
// purely a flag other systems can read via Entity.IsInCombat (e.g. Bog Iris's regen doubling
// while out of combat) without each of them tracking their own "last hit" timer
public class InCombatEffect : StatusEffect
{
    public const float Duration = 4f;

    public InCombatEffect(Entity target) : base(target, Duration, 1, target)
    {
        effectType = Type.neutral;
        visible = false;
    }

    public override void OnApply() { }
    public override void OnExpire() { }
    public override void OnTick(float deltaTime) { }

    public override string GetName() => "In Combat";
    public override string GetDescription() => "";
}
