using UnityEngine;

[CreateAssetMenu(fileName = "BegoniaData", menuName = "Scriptable Objects/PlantData/Begonia")]
public class BegoniaData : PlantData
{
    public float baseElementalAffinityBonus = 0.24f;
    public float baseAttackSpeedBonus;
    public float baseGrassDamageBonus = 0.2f;
    public float basePassiveMultiplier;
    public float baseSkillMultiplier;
    public float basePrimerCooldownReduction = 0.5f;
    public float baseGerminateSpeedup = 0.5f;
    [Tooltip("share of her own total elemental affinity and grass damage that Begonia's Blessing gives other plants, 0.85 = 85%")]
    public float baseBlessingShare = 0.85f;

    [Header("Path 1 Scaling")]
    public float path1AttackRangePerLevel = 0.2f;
    [Tooltip("flat seconds shaved off an insect's Water primer internal cooldown while standing in her flowers")]
    public float baseWaterCooldownReduction = 2f;
    public float path1CooldownReductionPerLevel = 0.6f;
    [Tooltip("Grass Resistance reduction on insects standing in her flowers")]
    public float baseGrassResistanceShred = 0.12f;
    public float path1GrassResistanceShredPerLevel = 0.04f;

    [Header("Path 2 Scaling")]
    public float path2ElementalAffinityPerLevel = 0.08f;
    public float path2GrassDamagePerLevel = 0.04f;
    public float path2GerminateSpeedupPerLevel = 0.1f;
    public float path2BlessingSharePerLevel = 0.05f;

    [Header("Path 3 Scaling")]
    public float path3AttackSpeedBonusPerLevel = 0.04f;
    public float path3SkillDurationPerLevel = 1f;
    public float path3PrimerCooldownReductionPerLevel = 0.1f;

    [Header("Range Glitter")]
    [Tooltip("glints that rise and fade all over her attack range while she isn't selected, marking it. empty uses the ground mulches' glint (MulchGlitter)")]
    public Material rangeGlitterMaterial;
    [Tooltip("each glint gets a colour between these two")]
    public Color rangeGlitterColorA = new Color(0.31f, 0.76f, 0.97f);
    public Color rangeGlitterColorB = new Color(0.70f, 0.90f, 0.99f);
    [Tooltip("glints per second for each square unit of her range, so a bigger range isn't thinner")]
    public float rangeGlitterDensity = 0.8f;
    [Tooltip("seconds a glint lasts, min and max")]
    public Vector2 rangeGlitterLifetime = new Vector2(1f, 1.6f);
    [Tooltip("world size of a glint, min and max")]
    public Vector2 rangeGlitterSize = new Vector2(0.07f, 0.12f);
    [Tooltip("how fast a glint rises, units per second, min and max")]
    public Vector2 rangeGlitterRiseSpeed = new Vector2(0.25f, 0.5f);

    public override string GetAttackDescription() =>
        "Grows wet flowers around her, applying <color=#4FC3F7><b>Water</b></color> primer to insects in contact with them, reducing its internal cooldown and their <color=green><b>Grass Resistance</b></color>.";

    public override string GetPassiveDescription() =>
        "Grants herself base <color=green><b>Elemental Affinity</b></color> and <color=green><b>Grass Damage</b></color>. Other plants within her attack radius are granted <color=#4FC3F7><b>Begonia's Blessing</b></color>, sharing her own <color=green><b>Elemental Affinity</b></color> and <color=green><b>Grass Damage</b></color> and making their <color=green><b>Germinate</b></color> triggers detonate earlier. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";

    public override string GetSkillDescription() =>
        "Plants within her attack range are granted <color=green><b>Blossoming</b></color>, increasing <color=green><b>Attack Speed</b></color>. While active, dealing <color=#4FC3F7><b>Water</b></color> or <color=green><b>Grass</b></color> damage reduces that insect's own primer cooldown for that element. Scales with <color=#FFB6C1><b>Magic Power</b></color>.";
}
