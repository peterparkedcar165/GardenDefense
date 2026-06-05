using UnityEngine;

[CreateAssetMenu(fileName = "GhostFungusData", menuName = "Scriptable Objects/PlantData/GhostFungus")]
public class GhostFungusData : PlantData
{
    [Header("Path 1 Attack")]
    public float path1AttackDamagePerLevel       = 4f;     // the fungus' own bolt damage per level

    [Header("Shroomlet Core Stats (base + per level + magic power)")]
    public float shroomletBaseAttackDamage       = 8f;
    public float shroomletAttackDamagePerLevel   = 2f;     // path 1
    public float shroomletAttackDamageMP         = 0f;     // x magic power

    public float shroomletBaseAttackSpeed        = 1f;
    public float shroomletAttackSpeedPerLevel    = 0.05f;  // path 1
    public float shroomletAttackSpeedMP          = 0f;

    public float shroomletBaseHealth             = 50f;
    public float shroomletHealthPerLevel         = 10f;    // path 2
    public float shroomletHealthMP               = 0f;

    [Header("Path 2 Shroomlets")]
    public int   baseShroomletCount             = 1;
    public int   path2ShroomletPerLevel         = 1;
    public float path2SpawnCooldownReducerPerLevel = 0.5f;   // flat seconds shaved off the spawn timer per level
    public float shroomletAttackRange      = 0.5f;
    public float shroomletDetectionRange   = 2.5f;   // how far a shroomlet spots enemies (from itself)
    public float shroomletMoveSpeed        = 1.5f;   // how fast it chases / returns to its post

    [Header("Path 3 Fungal Hypnosis")]
    public float skillWaveSpeed            = 14f;
    public float skillWaveWidth            = 1.5f;
    public float skillThickness            = 1f;
    public float skillTravelDistance       = 12f;
    public float baseFungalHealthMultiplier    = 1f;     // base health bonus on the turned insect (1 = +100%)
    public float path3HealthMultiplierPerLevel = 0.2f;   // extra health bonus per skill level
    public float baseFungalAttackMultiplier    = 1f;     // base attack damage bonus on the turned insect
    public float path3AttackMultiplierPerLevel = 0.2f;   // extra attack damage bonus per skill level
    public float baseFungalMoveSlow        = 0.1f;   // the turned insect's own movement speed is reduced by this
    public float path3MoveSlowPerLevel     = 0.05f;  // extra movement slow per skill level

    public override string GetAttackDescription() =>
        $"Fires a bolt of ice dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> damage to the first insect hit.";

    public override string GetPassiveDescription() =>
        $"Conjures a Ghost Shroomlet every <color=green><b>{basePassiveCooldown:F0}s</b></color> (up to <color=green><b>{baseShroomletCount}</b></color>) that holds position until an enemy comes into sight, then engages it with <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> attacks dealing <color=green><b>{shroomletBaseAttackDamage:F0}</b></color> damage (<color=green><b>{shroomletBaseHealth:F0}</b></color> HP).";

    public override string GetSkillDescription() =>
        $"Sends a spectral wave that inflicts <color=#00FFFF>Fungal Hypnosis</color> on the first insect hit, permanently turning it against its own with <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> strikes, granting <color=green><b>+{baseFungalHealthMultiplier * 100f:F0}%</b></color> Max Health and <color=green><b>+{baseFungalAttackMultiplier * 100f:F0}%</b></color> Attack Damage.";
}
