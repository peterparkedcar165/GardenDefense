using UnityEngine;

// Rhodiola's revival heal past the revived plant's max health, kept as a shield (Tommy, 2026-11-27): as much as the
// overheal, for her Verdant Guard duration
public class VerdantGuardEffect : ShieldEffect
{
    public VerdantGuardEffect(Entity target, float duration, Entity source, float amount)
        : base(target, duration, 1, source, amount)
    {
        elementalType = ElementalType.Grass;
    }

    public override string GetName() => "<color=green><b>Verdant Guard</b></color>";
}
