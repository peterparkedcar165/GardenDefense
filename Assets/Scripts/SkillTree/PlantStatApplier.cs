using UnityEngine;

// shared stat application used by fertilizers and skill trees
// pushes a value into the matching adder or multiplier field on a plant
public static class PlantStatApplier
{
    public static void Apply(Plant plant, StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.AttackDamage:    plant.attackDamageMultiplier           += value; break;
            case StatType.AttackSpeed:     plant.attackSpeedMultiplier            += value; break;
            case StatType.AttackRange:     plant.attackRangeMultiplier            += value; break;
            case StatType.FireDamage:      plant.fireDamageAdder             += value; break;
            case StatType.IceDamage:       plant.iceDamageAdder              += value; break;
            case StatType.WaterDamage:     plant.waterDamageAdder            += value; break;
            case StatType.GrassDamage:    plant.grassDamageAdder           += value; break;
            case StatType.PoisonDamage:    plant.poisonDamageAdder           += value; break;
            case StatType.WindDamage:      plant.windDamageAdder             += value; break;
            case StatType.GroundDamage:    plant.groundDamageAdder           += value; break;
            case StatType.BonusEffectChance: plant.bonusEffectChanceAdder    += value; break;
            case StatType.ElementalEffectChance: plant.elementalEffectChanceAdder += value; break;
            case StatType.MinimumDamage:   plant.minimumDamageAdder          += value; break;
            case StatType.MaximumDamage:   plant.maximumDamageAdder          += value; break;
            case StatType.CriticalChance:  plant.criticalChanceAdder         += value; break;
            case StatType.CriticalDamage:  plant.criticalDamageAdder         += value; break;
            case StatType.elementalAffinity:  plant.elementalAffinityAdder         += value; break;
            case StatType.PassiveDamage:   plant.passiveDamageAdder          += value; break;
            case StatType.SkillDamage:     plant.skillDamageAdder            += value; break;
            case StatType.SkillCooldown:   plant.skillCooldownReductionMultiplier += value; break;
            case StatType.DoTDamage:       plant.dotDamageAdder              += value; break;
            case StatType.Piercing:                     if (plant is Shooter shooter) shooter.piercingAdder += Mathf.RoundToInt(value); break;
            case StatType.ImmobilizeDurationAdder:      plant.immobilizeDurationAdder      += value; break;
            case StatType.ImmobilizeDurationMultiplier: plant.immobilizeDurationMultiplier += value; break;
            case StatType.PassiveCooldown:              plant.passiveCooldownReductionMultiplier += value; break;
            case StatType.PassiveDurationMultiplier:    plant.passiveDurationMultiplier          += value; break;
            case StatType.SkillDurationAdder:           plant.skillDurationAdder      += value; break;
            case StatType.SkillDurationMultiplier:      plant.skillDurationMultiplier      += value; break;
            case StatType.CoordinatedDamage:            plant.coordinatedDamageAdder       += value; break;
            case StatType.HealingBonus:                 plant.healingBonusAdder            += value; break;
            case StatType.IlluminationRangeAdder:       plant.lightEmissionRangeAdder      += value; break;
            case StatType.IlluminationRangeMultiplier:  plant.lightEmissionRangeMultiplier += value; break;
            case StatType.CounterDamage:                plant.counterDamageAdder           += value; break;
            case StatType.PhysicalDamage:               plant.physicalDamageAdder          += value; break;
            case StatType.MagicDamage:                  plant.magicDamageAdder             += value; break;
            case StatType.PhysicalResistance:           plant.armorAdder      += value; break;
            case StatType.MagicResistance:              plant.magicArmorAdder += value; break;
            case StatType.MagicPower:                   plant.magicPowerAdder              += value; break;
            case StatType.DebuffGivenDuration:          plant.debuffGivenDurationAdder     += value; break;
            case StatType.BuffGivenDuration:            plant.buffGivenDurationAdder       += value; break;
            case StatType.BuffReceivedDuration:         plant.buffReceivedDurationAdder    += value; break;
            case StatType.DebuffReceivedDuration:       plant.debuffReceivedDurationAdder  += value; break;
            case StatType.MinionDamage:                 plant.minionDamageAdder            += value; break;
            case StatType.FallDamage:                   plant.fallDamageAdder              += value; break;
            case StatType.Armor:                        plant.armorAdder                   += value; break;
            case StatType.MagicArmor:                   plant.magicArmorAdder              += value; break;
            case StatType.ArmorPenetration:             plant.armorPenFlatAdder            += value; break;
            case StatType.MagicPenetration:             plant.magicPenFlatAdder            += value; break;
            case StatType.ArmorShred:                   plant.armorPenPercentAdder         += value; break;
            case StatType.MagicArmorShred:              plant.magicPenPercentAdder         += value; break;
            case StatType.DoTDuration:                  plant.dotDurationAdder             += value; break;
            case StatType.RegenerationDuration:         plant.regenerationDurationAdder    += value; break;
            case StatType.ShieldDuration:               plant.shieldDurationAdder          += value; break;
            case StatType.SunGenerationCooldownMultiplier:  plant.sunGenerationCooldownMultiplier += value; break;
            case StatType.MaxHealth:                    plant.maxHealthMultiplier          += value; break;
            case StatType.SunYield:                     plant.sunYieldMultiplier           += value; break;
            case StatType.CurrencyYield:                plant.currencyYieldMultiplier      += value; break;
        }
    }
}
