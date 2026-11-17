using UnityEngine;

// replaced golden root (tommy, 2026-11-16): its armor only repeated rejuvenating burgeon's
[CreateAssetMenu(fileName = "SpreadingSeedMulchData", menuName = "Scriptable Objects/Mulch/Spreading Seed")]
public class SpreadingSeedMulchData : MulchData
{
    [Tooltip("share of the seed's heal the second plant gets, e.g. 0.4 = 40%")]
    public float healShare = 0.4f;
    [Tooltip("how far from the seed's target the second plant can stand, in tiles (a tile is 1 unit). " +
        "1 reaches the tiles right beside it; diagonal tiles are about 1.4 away")]
    public float radiusTiles = 1f;

    public override MulchEffect CreateEffect() => new SpreadingSeedMulchEffect(this);

    public override string GetDescription()
    {
        string tiles = Mathf.Approximately(radiusTiles, 1f) ? "tile" : "tiles";
        return $"{TargetPlantLabel("Rhodiola")}'s healing seed also heals the most injured plant within " +
            $"<b><color=green>{radiusTiles:0.#}</color></b> {tiles} of its target for <b><color=green>{healShare * 100f:F0}%</color></b> as much.";
    }
}
