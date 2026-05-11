using UnityEngine;

public class FertilizerManager : MonoBehaviour
{
    public static FertilizerManager instance;

    private FertilizerData activeFertilizer;
    private float[] rolledValues;

    void Awake()
    {
        instance = this;
    }

    public void SelectFertilizer(FertilizerData fertilizer)
    {
        activeFertilizer = fertilizer;
        rolledValues = new float[fertilizer.stats.Length]; // length is the size of array
        for (int i = 0; i < fertilizer.stats.Length; i++)
        {
            FertilizerStat stat = fertilizer.stats[i];
            rolledValues[i] = Random.Range(stat.minValue, stat.maxValue);
        }
    }

    public void ApplyTo(Plant plant)
    {
        if (activeFertilizer == null) return; // if null, cancel

        if (!activeFertilizer.appliesToAll && plant.plantType != activeFertilizer.targetPlantType) return;
        // if does not apply to all AND the plant type is not the same as the feritlizer's targets, cancel

        for (int i = 0; i < activeFertilizer.stats.Length; i++) // for each slot of the given stats.
            ApplyStat(plant, activeFertilizer.stats[i].statType, rolledValues[i]);
    }

    private void ApplyStat(Plant plant, StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.AttackDamage:    plant.attackDamageAdder    += value; break;
            case StatType.AttackSpeed:     plant.attackSpeedAdder     += value; break;
            case StatType.AttackRange:     plant.attackRangeAdder     += value; break;
            case StatType.FireDamage:      plant.fireDamageAdder      += value; break;
            case StatType.IceDamage:       plant.iceDamageAdder       += value; break;
            case StatType.WaterDamage:     plant.waterDamageAdder     += value; break;
            case StatType.NatureDamage:    plant.natureDamageAdder    += value; break;
            case StatType.PoisonDamage:    plant.poisonDamageAdder    += value; break;
            case StatType.WindDamage:      plant.windDamageAdder      += value; break;
            case StatType.CriticalChance:  plant.criticalChanceAdder  += value; break;
            case StatType.CriticalDamage:  plant.criticalDamageAdder  += value; break;
            case StatType.ElementalPower:  plant.elementalPowerAdder  += value; break;
            case StatType.PassiveDamage:   plant.passiveDamageAdder   += value; break;
            case StatType.SkillDamage:     plant.skillDamageAdder     += value; break;
            case StatType.SkillCooldown:   plant.skillCooldownReductionAdder     += value; break;
        }
    }
}
