using UnityEngine;
using System.Collections.Generic;

// base class for Burgeon plants that summon Minions onto the path. handles instantiating
// minions, tracking the live roster, and capping how many can exist at once. concrete plants
// decide WHEN and WHERE to summon (timer, target tile, etc.) by calling SummonMinion
public abstract class SummonerPlant : Plant
{
    [SerializeField] protected GameObject minionPrefab;   // a prefab with a Minion component + InsectData
    protected readonly List<Minion> minions = new List<Minion>();

    // spawns a minion at the given world position with the given lifetime, tracks it, and
    // returns it so the caller can tweak its stats (e.g. scale damage with the plant's level)
    protected Minion SummonMinion(Vector3 position, float lifetime)
    {
        if (minionPrefab == null) return null;
        GameObject go = Instantiate(minionPrefab, position, Quaternion.identity);
        Minion minion = go.GetComponent<Minion>();
        if (minion == null) { Destroy(go); return null; }
        minion.Initialize(lifetime);
        minions.Add(minion);
        return minion;
    }

    // number of minions still alive (prunes dead/destroyed entries)
    protected int ActiveMinionCount()
    {
        minions.RemoveAll(m => m == null || !m.IsAlive);
        return minions.Count;
    }

    // nearest point on the main path to a world position, a handy default summon spot
    protected Vector3 NearestPathPoint(Vector3 from)
    {
        Transform[] wps = PathManager.instance != null ? PathManager.instance.waypoints : null;
        if (wps == null || wps.Length == 0) return from;
        Vector3 best = wps[0].position;
        float bestDist = Mathf.Infinity;
        foreach (Transform wp in wps)
        {
            if (wp == null) continue;
            float d = Vector3.Distance(from, wp.position);
            if (d < bestDist) { bestDist = d; best = wp.position; }
        }
        return best;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // when the plant is removed, dismiss its minions
        foreach (Minion m in minions)
            if (m != null && m.IsAlive) m.Kill();
        minions.Clear();
    }
}
