using UnityEngine;

// Thorn family only: a flat Attack Speed bonus, plus flat bonus damage tacked onto every attack
// hit (same damage type/element as the attacker, scaled by its own onHitEffectiveness like any
// other on-hit bonus - see Entity.HandleOnHitEffects/BirdOfParadise.OnAttackHit for the pattern)
public class RapidStrikesMulchData : MulchData
{
    [Tooltip("additive Attack Speed, e.g. 0.25 = 25%")]
    public float attackSpeedBonus = 0.25f;
    [Tooltip("flat bonus damage added to every attack hit, same type/element as the attack itself")]
    public float flatOnHitDamage = 15f;

    public override MulchEffect CreateEffect() => new RapidStrikesMulchEffect(this);
}
