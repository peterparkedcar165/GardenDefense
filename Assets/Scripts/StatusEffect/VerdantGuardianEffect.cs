using UnityEngine;

public class VerdantGuardianEffect : ShieldEffect
{
    private readonly float _regenPerSecond;
    private float _tickTimer;

    public VerdantGuardianEffect(Entity target, float duration, Entity source, float shieldAmount, float regenPerSecond)
        : base(target, duration, 1, source, shieldAmount)
    {
        _regenPerSecond = regenPerSecond;
        elementalType = ElementalType.Grass;
    }

    public override void OnTick(float deltaTime)
    {
        if (amount <= 0f) return;
        _tickTimer += deltaTime;
        if (_tickTimer < 1f) return;
        _tickTimer -= 1f;
        target.Heal(_regenPerSecond, source);
    }

    protected override string GetShieldDetails() =>
        $"Regenerating <color=green><b>{_regenPerSecond:F0}</b></color> health per second while the shield holds.";

    public override string GetName() => "<color=green><b>Verdant Guardian</b></color>";
}
