using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Tilemaps;

// decoration tiers of high ground (Tommy, 2026-11-28): each tier is its own raised map (HighgroundGround2, 3 and up)
// with its own cliff maps (CliffFaces2, CliffTops2 ...) drawn over the tier below, so cliffs can stand on top of or
// beside each other. a tier changes nothing in play: no grid manager reads its map and its CliffBuilder lays no walls
// (buildEdges off). a cell on a higher tier counts as raised for every decoration tier under it, so a tier 3 hill on
// a tier 2 plateau sits inside it; the level's own high ground (HighgroundGround) is left as it is.
// sorting, on the cliffs' layer (Non-Colliders_Ground): the level's cliffs are faces 0 and tops 1, tier 2 is ground 2,
// faces 3, tops 4, and each tier after sits 3 orders above the one before, all under the trees
public static class HighgroundTiers
{
    private const string Undo_ = "Add Highground Tier";

    [MenuItem("Tools/Garden Defense/Add Highground Tier")]
    private static void AddTier()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        CliffBuilder[] builders = Object.FindObjectsByType<CliffBuilder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        CliffBuilder level = builders.FirstOrDefault(b => b.buildEdges);
        if (level == null || level.tops == null || level.faces == null || level.raisedMaps == null
            || level.raisedMaps.Length == 0 || level.raisedMaps[0] == null)
        {
            EditorUtility.DisplayDialog(Undo_, "This level has no cliffs to build on (no CliffBuilder with its maps).", "OK");
            return;
        }

        Transform grid = level.tops.transform.parent;
        int n = 2;
        while (grid.Find("HighgroundGround" + n) != null) n++;

        Tilemap ground = level.raisedMaps[0];
        TilemapRenderer topsLook = level.tops.GetComponent<TilemapRenderer>();
        int layer = topsLook.sortingLayerID;
        int at = level.tops.transform.GetSiblingIndex() + 1;
        foreach (CliffBuilder t in builders.Where(b => !b.buildEdges))
            at = Mathf.Max(at, t.transform.GetSiblingIndex() + 1);

        Tilemap raised = NewMap(grid, "HighgroundGround" + n, ground, ground.GetComponent<TilemapRenderer>(), layer, 3 * n - 4, at);
        Tilemap faces = NewMap(grid, "CliffFaces" + n, level.faces, level.faces.GetComponent<TilemapRenderer>(), layer, 3 * n - 3, at + 1);
        Tilemap tops = NewMap(grid, "CliffTops" + n, level.tops, topsLook, layer, 3 * n - 2, at + 2);

        // the lower decoration tiers count this tier's cells as raised too
        foreach (CliffBuilder t in builders.Where(b => !b.buildEdges))
        {
            Undo.RecordObject(t, Undo_);
            t.raisedMaps = (t.raisedMaps ?? new Tilemap[0]).Append(raised).ToArray();
            t.Rebuild();
        }

        CliffBuilder builder = Undo.AddComponent<CliffBuilder>(tops.gameObject);
        builder.cliffSet = level.cliffSet;
        builder.boundsMap = level.boundsMap;
        builder.raisedMaps = new[] { raised };
        builder.tops = tops;
        builder.faces = faces;
        builder.buildEdges = false;
        builder.Rebuild();

        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        Selection.activeGameObject = raised.gameObject;
        Debug.Log($"[Highground] tier {n} added: paint its ground on {raised.name} (now selected), the cliffs draw themselves on {faces.name} and {tops.name}");
    }

    // a LowCollider map beside Collider in the open level, for a level that has none (Tommy, 2026-11-28: Level 6 was
    // open when every other level got one): an empty copy of Collider's setup, its cells obstacles that don't block sight
    [MenuItem("Tools/Garden Defense/Add LowCollider")]
    private static void AddLowCollider()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Tilemap collider = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(m => m.name == "Collider");
        if (collider == null) { EditorUtility.DisplayDialog("Add LowCollider", "This scene has no Collider tilemap.", "OK"); return; }
        Transform grid = collider.transform.parent;
        if (grid.Find("LowCollider") != null) { EditorUtility.DisplayDialog("Add LowCollider", "This level already has a LowCollider.", "OK"); return; }
        TilemapRenderer look = collider.GetComponent<TilemapRenderer>();
        Tilemap low = NewMap(grid, "LowCollider", collider, look, look.sortingLayerID, look.sortingOrder, collider.transform.GetSiblingIndex() + 1);
        Undo.SetCurrentGroupName("Add LowCollider");
        EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        Selection.activeGameObject = low.gameObject;
        Debug.Log("[LowCollider] added beside Collider (now selected): its cells are obstacles that don't block sight");
    }

    private static Tilemap NewMap(Transform parent, string name, Tilemap like, TilemapRenderer rendererLike, int sortingLayer, int order, int sibling)
    {
        GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        Undo.RegisterCreatedObjectUndo(go, Undo_);
        go.layer = like.gameObject.layer;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = like.transform.localPosition;
        go.transform.SetSiblingIndex(Mathf.Min(sibling, parent.childCount - 1));
        Tilemap map = go.GetComponent<Tilemap>();
        map.tileAnchor = like.tileAnchor;
        map.orientation = like.orientation;
        map.animationFrameRate = like.animationFrameRate;
        map.color = like.color;
        TilemapRenderer r = go.GetComponent<TilemapRenderer>();
        if (rendererLike != null && ComponentUtility.CopyComponent(rendererLike)) ComponentUtility.PasteComponentValues(r);
        r.sortingLayerID = sortingLayer;
        r.sortingOrder = order;
        return map;
    }
}
