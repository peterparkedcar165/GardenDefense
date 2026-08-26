using UnityEngine;

[CreateAssetMenu(fileName = "StargazerData", menuName = "Scriptable Objects/PlantData/Stargazer")]
public class StargazerData : PlantData
{
    [Header("Attack Cone")]
    public float coneAngle = 60f;   // total width of the flamethrower cone, in degrees

    [Header("Passive Flammable")]
    public float flammableBonusPerStack = 0.01f;   // +X% Burn damage per stack
    public int   flammableMaxStacks     = 5;        // fixed stack cap
    public float flammableDuration      = 4f;
    public int   baseStacksPerHit       = 1;        // stacks applied per hit at base
    public float baseBurnDurationBonus  = 0.5f;     // Burns the Stargazer causes last (1 + this)x longer
    public float passiveProcChance      = 0.25f;    // chance for a Fire Damage hit to apply Flammable at all

    [Header("Skill Sweep (directional fire wave)")]
    public float skillBaseDamage = 200f;
    public float skillWaveSpeed  = 12f;
    public float skillLength     = 50f;    // total length of the lane, covers the whole screen (plant is the pivot)
    public float skillWaveWidth  = 4f;     // circle diameter; the wave's radius is half this
    public float skillDelay      = 1f;
    public float skillBurnMultiplier = 2f; // damage multiplier against Burning targets
    public int   skillFlammableStacks = 2; // Flammable stacks the wave applies on hit
    public float skillBurnDuration = 6f;   // duration of the guaranteed Burn applied by each wave hit

    [Header("Path 1 Scaling")]
    public float path1AttackDamagePerLevel = 3f;
    public float path1AttackRangePerLevel  = 0.2f;
    public float path1MaxFireDamageBonus   = 0.25f;
    public float path1MaxConeAngleBonus    = 15f;

    [Header("Path 2 Scaling")]
    public int   path2StacksPerLevel       = 1;     // extra Flammable stacks applied per hit per passive level
    public float path2BurnDurationPerLevel = 0.1f;  // Burn duration multiplier gained per passive level
    public float path2ProcChancePerLevel   = 0.05f; // proc chance gained per passive level
    public float path2MaxElementalAffinityBonus     = 0.2f;
    public float path2MaxElementalEffectChanceBonus = 0.06f;

    [Header("Path 3 Scaling")]
    public float path3SkillDamagePerLevel       = 40f;
    public float path3WaveWidthPerLevel         = 0.5f;   // lane width gained per skill level
    public float path3BurnMultiplierPerLevel    = 0.1f;   // burn-damage multiplier gained per skill level
    public float path3FlammableStacksPerLevel   = 0.5f;   // extra wave Flammable stacks per skill level

    public override string GetAttackDescription() =>
        $"Sprays fire in a cone, dealing damage to all insects within it.";

    public override string GetPassiveDescription() =>
        $"Dealing Fire Damage has a chance to inflict <color=#FF6B1A>Flammable</color>, increasing the <color=orange>Burn</color> damage it takes.";

    public override string GetSkillDescription() =>
        $"Aim a direction. After a brief delay, a wall of fire sweeps across the entire map, dealing massive damage and inflicting Burn on everything in its path.";
}
