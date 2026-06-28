using UnityEngine;

[CreateAssetMenu(fileName = "AnemoneData", menuName = "Scriptable Objects/PlantData/Anemone")]
public class AnemoneData : PlantData
{
    [Header("Splash")]
    public float splashRadius = 1.2f;

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 5f;
    public float path1AttackRangePerLevel  = 0.2f;

    [Header("Path 2 Scaling")]
    public int   baseInitialErosionStacks     = 3;
    public int   path2InitialStacksPerLevel   = 1;
    public float baseReductionPerStack        = 0.5f;

    [Header("Path 3 Scaling")]
    public float baseVortexRadius      = 1.5f;
    public float path3RadiusPerLevel   = 0.2f;
    public float vortexDuration        = 6f;
    public float vortexDamagePerTick   = 10f;
    public float vortexTickInterval    = 0.5f;
    public float vortexDragSpeed           = 0.8f;
    public float path3DragSpeedPerLevel    = 0.1f;
    public float vortexDetonationDamage = 150f;
    public float vortexKnockbackForce  = 8f;

    public override string GetAttackDescription() =>
        $"Launches a wind ball at the target, dealing <color=green><b>{baseAttackDamage}</b></color> {ElementalTag(elementalType)} {DamageTypeTag(damageType)} damage, and half that to surrounding insects.";

    public override string GetPassiveDescription() =>
        $"Dealing Wind Damage applies <color=green><b>{baseInitialErosionStacks}</b></color> stacks of <color=#E0E0E0><b>Wind Erosion</b></color> for <color=green><b>{basePassiveDuration:F0}s</b></color>, reducing <color=#00CED1><b>Armor</b></color> and <color=#9370DB><b>Magic Resist</b></color> by <color=red><b>{(int)baseReductionPerStack}</b></color> per stack. If the target is already afflicted with <color=#E0E0E0><b>Wind Erosion</b></color>, adds <color=green><b>1</b></color> stack instead.";

    public override string GetSkillDescription() =>
        $"Summon a vortex of wind at target location, dragging insects toward its center and dealing damage over time.";
}
