using UnityEngine;

[CreateAssetMenu(fileName = "GhostFungusData", menuName = "Scriptable Objects/PlantData/GhostFungus")]
public class GhostFungusData : PlantData
{
    [Header("Path 1 Attack")]
    public float path1AttackDamagePerLevel = 4f;
    public float path1AttackSpeedPerLevel  = 0.05f;

    [Header("Path 2 Shroomlets")]
    public int   baseShroomletCount        = 1;
    public int   path2ShroomletPerLevel    = 1;
    public float shroomletSpawnInterval    = 2f;     // delay before adding the first/next slot
    public float shroomletRespawnDelay     = 5f;     // delay after a death before respawning
    public float shroomletHealthFraction   = 0.5f;   // shroomlet maxHealth = fungus maxHealth * this
    public float shroomletDamageFraction   = 0.6f;   // shroomlet attackDamage = fungus attackDamage * this
    public float shroomletAttackSpeed      = 1f;
    public float shroomletAttackRange      = 0.5f;

    [Header("Path 3 Fungal Hypnosis")]
    public float skillWaveSpeed            = 14f;
    public float skillWaveWidth            = 1.5f;
    public float skillThickness            = 1f;
    public float skillTravelDistance       = 12f;
    public float fungalHealthMultiplier    = 1f;     // +100% health on the turned insect
    public float fungalAttackMultiplier    = 1f;     // +100% attack damage on the turned insect
    public float baseFungalSlowPercent     = 0.2f;   // its attacks slow enemies' attack speed by this
    public float path3SlowPerLevel         = 0.03f;
    public float fungalSlowDuration        = 2f;

    public override string GetAttackDescription() =>
        $"Fires a bolt of ice dealing <color=green><b>{baseAttackDamage:F0}</b></color> <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> damage to the first insect hit.";

    public override string GetPassiveDescription() =>
        $"Periodically conjures <color=green><b>{baseShroomletCount}</b></color> Ghost Shroomlet that holds position until an enemy comes into sight, then engages it with <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> attacks. Shroomlets inherit the fungus' stats and respawn <color=green><b>{shroomletRespawnDelay:F0}s</b></color> after dying.";

    public override string GetSkillDescription() =>
        $"Sends a spectral wave that inflicts <color=#B266FF>Fungal Hypnosis</color> on the first insect hit, permanently turning it against its own with <color=#00FFFF>Ice</color> <color=#A0522D>Physical</color> strikes that slow enemies' attack speed by <color=green><b>{baseFungalSlowPercent * 100f:F0}%</b></color>.";
}
