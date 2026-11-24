using UnityEngine;

[CreateAssetMenu(fileName = "AnemoneData", menuName = "Scriptable Objects/PlantData/Anemone")]
public class AnemoneData : PlantData
{
    [Header("Splash")]
    public float splashRadius = 1.2f;
    [Tooltip("splash damage hits at most this many of the nearest insects around the main target (no cap on the main target)")]
    public int splashMaxExtraTargets = 3;

    [Header("Path 1 Scaling")]
    public float path1AttackSpeedPerLevel = 0.05f;
    public float path1AttackRangePerLevel = 0.2f;

    [Header("Path 2 Scaling")]
    public int   baseInitialErosionStacks     = 3;
    public int   path2InitialStacksPerLevel   = 1;
    public float baseReductionPerStack        = 0.5f;
    public float baseErosionProcChance        = 0.5f;
    public float path2ProcChancePerLevel      = 0.05f;
    public float erosionDurationPerLevel      = 1f;

    [Header("Path 3 Scaling")]
    public float baseVortexRadius      = 1.5f;
    public float path3RadiusPerLevel   = 0.2f;
    public float vortexDamagePerTick   = 10f;
    public float vortexTickInterval    = 0.5f;
    public float vortexDragSpeed           = 0.8f;
    public float path3DragSpeedPerLevel    = 0.1f;
    public float vortexDetonationDamage = 150f;
    public float vortexKnockbackForce  = 8f;
}
