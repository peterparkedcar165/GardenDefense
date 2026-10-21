using UnityEngine;

// one class, many assets - make one asset per family with the desired PlantFamily set in the
// Inspector, same "one class, many assets" pattern WeatherMulchData uses for its weather variants
public class FamilySkillCooldownMulchData : MulchData
{
    public PlantFamily family;
    [Tooltip("additive Skill Cooldown Reduction, e.g. 0.25 = 25%")]
    public float cooldownReductionPercent = 0.25f;

    public override MulchEffect CreateEffect() => new FamilySkillCooldownMulchEffect(this);
}
