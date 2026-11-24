using UnityEngine;

public class VerdantGuardianEffect : ShieldEffect
{
    // one shared tag array for every instance
    private static readonly EffectTag[] RegenTags = { EffectTag.Regenerative };

    private readonly float _regenPerSecond;
    // the shared over time timing (OverTimeTicker): a tick every second
    private readonly OverTimeTicker _ticks = new OverTimeTicker(1f);

    public VerdantGuardianEffect(Entity target, float duration, Entity source, float shieldAmount, float regenPerSecond)
        : base(target, duration, 1, source, shieldAmount)
    {
        _regenPerSecond = regenPerSecond;
        elementalType = ElementalType.Grass;
        tags = RegenTags;
    }

    public override void OnTick(float deltaTime)
    {
        if (amount <= 0f) return;
        int due = _ticks.Advance(deltaTime);
        for (int i = 0; i < due; i++) HealTarget(_regenPerSecond * _ticks.baseInterval);
    }

    public override void OnNaturalExpire()
    {
        if (amount <= 0f) return;
        float share = _ticks.LeftoverShare(duration);
        if (share > 0.0001f) HealTarget(_regenPerSecond * _ticks.baseInterval * share);
    }

    protected override string GetShieldDetails() =>
        $"Regenerating <color=green><b>{_regenPerSecond:F0}</b></color> health per second while the shield holds.";

    public override string GetName() => "<color=green><b>Verdant Guardian</b></color>";
}
